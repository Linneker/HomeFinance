using System.Globalization;
using Acme.Sistemas.Domain.Entities.Alertas;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Assinaturas;

namespace Acme.Sistemas.Services.V1.Alertas;

/// <inheritdoc cref="IAlertaService"/>
public sealed class AlertaService(
    IAlertaWriteRepository alertas,
    IRelatorioReadRepository relatorios,
    IOrcamentoReadRepository orcamentos,
    ICategoriaReadRepository categorias,
    IFonteRendaReadRepository fontes,
    IContextoRequisicao contexto,
    IAssinaturaService assinaturas,
    TimeProvider relogio)
    : IAlertaService
{
    /// <summary>
    /// Quantos dias à frente a projeção de caixa olha.
    /// </summary>
    /// <remarks>
    /// Quarenta e cinco dias cobrem o próximo salário e a fatura seguinte, que é o horizonte em
    /// que ainda dá para agir. Uma janela muito maior encheria a tela de faltas projetadas em
    /// datas que a pessoa nem pensou ainda, e o alerta viraria ruído.
    /// </remarks>
    public const int DiasDeProjecao = 45;

    public async Task<ResultadoDaAvaliacao> AvaliarCompetenciaAsync(
        DateOnly competencia,
        CancellationToken cancellationToken)
    {
        // Somente leitura significa tenant recebido por compartilhamento: o alerta é do dono,
        // não de quem está olhando. Emitir aqui gravaria alerta no espaço alheio — a RLS
        // recusaria a escrita, e o convidado veria um erro que não é dele.
        if (!PodeAvaliar() || !await AssinaturaVigenteAsync(cancellationToken))
        {
            return ResultadoDaAvaliacao.Nenhum;
        }

        var mes = CalendarioFinanceiro.PrimeiroDiaDoMes(competencia);

        var periodo = new PeriodoDoRelatorio(
            mes, CalendarioFinanceiro.UltimoDiaDoMes(mes), PorCaixa: false);

        var totais = await relatorios.TotaisPorCategoriaAsync(periodo, cancellationToken);
        var planejado = await orcamentos.ListarDoMesAsync(mes, cancellationToken);
        var nomes = (await categorias.ListarAsync(cancellationToken))
            .ToDictionary(categoria => categoria.Id, categoria => categoria.Nome);

        var realizado = totais
            .Where(total => total.CategoriaId is not null)
            .ToDictionary(total => total.CategoriaId!.Value, total => total.Realizado);

        // Categoria sem orçamento não entra: não há do que estourar. Alertar sobre ela
        // transformaria "ainda não planejei isso" em problema, e o produto passaria a
        // repreender quem está começando.
        var condicoes = planejado
            .Where(item => realizado.GetValueOrDefault(item.CategoriaId) > item.ValorPlanejado)
            .ToDictionary(
                item => ChaveDeCondicao.OrcamentoEstourado(item.CategoriaId, mes),
                item => MensagemDeEstouro(
                    nomes.GetValueOrDefault(item.CategoriaId, "Categoria"),
                    mes,
                    item.ValorPlanejado,
                    realizado[item.CategoriaId]));

        return await ConciliarAsync(
            TipoAlerta.Competencia,
            condicoes,
            chave => chave.StartsWith("orcamento:", StringComparison.Ordinal)
                     && chave.EndsWith(mes.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                         StringComparison.Ordinal),
            cancellationToken);
    }

    public async Task<ResultadoDaAvaliacao> AvaliarCaixaAsync(CancellationToken cancellationToken)
    {
        if (!PodeAvaliar() || !await AssinaturaVigenteAsync(cancellationToken))
        {
            return ResultadoDaAvaliacao.Nenhum;
        }

        var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        var ate = hoje.AddDays(DiasDeProjecao);

        var saidas = await relatorios.ExtratoAsync(
            new PeriodoDoRelatorio(hoje, ate, PorCaixa: true), cancellationToken);

        var vigentes = await fontes.ListarAsync(null, cancellationToken);

        var pontos = ProjecaoDeCaixa.Projetar(
            hoje,
            ate,
            vigentes,
            saidas.Select(saida => new SaidaPrevista(saida.DataCaixa, saida.ValorApurado)));

        var falta = ProjecaoDeCaixa.PrimeiraInsuficiencia(pontos);

        var condicoes = falta is null
            ? []
            : new Dictionary<string, string>
            {
                [ChaveDeCondicao.SaldoInsuficiente(falta.Data)] =
                    MensagemDeCaixa(falta.Data, falta.Diferenca),
            };

        // Só as condições da própria janela são conciliadas. Uma insuficiência projetada para
        // além do horizonte não foi avaliada — encerrá-la por ausência diria à pessoa que o
        // problema sumiu quando ele apenas saiu de vista.
        return await ConciliarAsync(
            TipoAlerta.Caixa,
            condicoes,
            chave => DentroDaJanela(chave, hoje, ate),
            cancellationToken);
    }

    /// <summary>
    /// Emite o que é novo, encerra o que deixou de valer e ignora o que já é conhecido.
    /// </summary>
    /// <param name="avaliada">
    /// Diz se aquela chave esteve sob avaliação agora. Encerrar tudo que não apareceu nas
    /// condições fecharia alertas de meses ou datas que esta chamada nem olhou.
    /// </param>
    private async Task<ResultadoDaAvaliacao> ConciliarAsync(
        TipoAlerta tipo,
        IReadOnlyDictionary<string, string> condicoes,
        Func<string, bool> avaliada,
        CancellationToken cancellationToken)
    {
        var existentes = await alertas.ListarPorCondicaoAtivaAsync(tipo, cancellationToken);
        var agora = relogio.GetUtcNow();

        var porChave = existentes
            .Where(alerta => alerta.CondicaoAtiva)
            .GroupBy(alerta => alerta.ChaveCondicao)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());

        var encerrados = 0;

        foreach (var (chave, grupo) in porChave)
        {
            if (condicoes.ContainsKey(chave) || !avaliada(chave))
            {
                continue;
            }

            foreach (var alerta in grupo)
            {
                // Encerra também o dispensado. Sem isso, a condição que voltasse a acontecer
                // nunca mais geraria aviso: o dispensado antigo bloquearia para sempre.
                alerta.Encerrar(agora);
                await alertas.AtualizarEstadoAsync(alerta, cancellationToken);
                encerrados++;
            }
        }

        var emitidos = 0;
        var conhecidos = 0;

        foreach (var (chave, mensagem) in condicoes)
        {
            if (porChave.ContainsKey(chave))
            {
                conhecidos++;
                continue;
            }

            await alertas.GravarAsync(
                new AlertaEntity
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = contexto.TenantId!.Value,
                    Tipo = tipo,
                    ChaveCondicao = chave,
                    Estado = EstadoAlerta.Aberto,
                    Mensagem = mensagem,
                    CriadoEm = agora,
                },
                cancellationToken);

            emitidos++;
        }

        return new ResultadoDaAvaliacao(emitidos, encerrados, conhecidos);
    }

    private bool PodeAvaliar() => contexto.TenantId is not null && !contexto.SomenteLeitura;

    /// <summary>
    /// Alerta é recurso derivado: com assinatura inativa nenhum é emitido nem encerrado.
    /// </summary>
    /// <remarks>
    /// Nem emitido <em>nem encerrado</em>: encerrar durante a suspensão apagaria o histórico de
    /// condições que a pessoa nunca chegou a ver, e ela voltaria do pagamento com a tela limpa
    /// achando que os problemas se resolveram sozinhos.
    /// </remarks>
    private async Task<bool> AssinaturaVigenteAsync(CancellationToken cancellationToken) =>
        (await assinaturas.SituacaoAtualAsync(cancellationToken)).Vigente;

    private static bool DentroDaJanela(string chave, DateOnly de, DateOnly ate) =>
        chave.StartsWith("caixa:", StringComparison.Ordinal)
        && DateOnly.TryParseExact(
            chave["caixa:".Length..],
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var data)
        && data >= de
        && data <= ate;

    /// <summary>
    /// A mensagem nomeia a categoria, o mês e quanto passou.
    /// </summary>
    /// <remarks>
    /// Sem o nome da categoria o aviso obrigaria a pessoa a abrir o relatório para descobrir do
    /// que ele fala, e o alerta perderia a única coisa que o torna acionável na notificação.
    /// </remarks>
    private static string MensagemDeEstouro(
        string categoria, DateOnly competencia, decimal planejado, decimal realizado) =>
        string.Create(
            CultureInfo.GetCultureInfo("pt-BR"),
            $"{categoria} passou do orçamento de {competencia:MM/yyyy} em " +
            $"{realizado - planejado:N2} (planejado {planejado:N2}, realizado {realizado:N2}).");

    private static string MensagemDeCaixa(DateOnly data, decimal diferenca) =>
        string.Create(
            CultureInfo.GetCultureInfo("pt-BR"),
            $"O saldo projetado para {data:dd/MM/yyyy} é insuficiente em {diferenca:N2}.");
}
