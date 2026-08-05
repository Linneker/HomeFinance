using Acme.Sistemas.Domain.Entities.Alertas;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Entities.Relatorios;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Alertas;
using Acme.Sistemas.Services.V1.Alertas.Command.DispensarAlerta;
using Acme.Sistemas.Services.V1.Alertas.Query.AlertasAbertos;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// As duas condições de alerta, o ciclo de vida delas e a entrega.
/// </summary>
public sealed class AlertaTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid Ana = Guid.CreateVersion7();
    private static readonly Guid Transporte = Guid.CreateVersion7();
    private static readonly Guid Alimentacao = Guid.CreateVersion7();

    private static readonly DateOnly Agosto = new(2026, 8, 1);
    private static readonly DateTimeOffset Agora = new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private sealed class ContextoDeTeste : IContextoRequisicao
    {
        public Guid? UsuarioId { get; set; } = Ana;

        public Guid? TenantId { get; set; } = Tenant;

        public bool SomenteLeitura { get; set; }
    }

    /// <summary>Guarda o que foi gravado e mantém o estado entre chamadas, como o banco.</summary>
    private sealed class AlertasEmMemoria : IAlertaWriteRepository
    {
        public List<AlertaEntity> Linhas { get; } = [];

        public Task<IReadOnlyList<AlertaEntity>> ListarPorCondicaoAtivaAsync(
            TipoAlerta tipo, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AlertaEntity>>(
                [.. Linhas.Where(alerta => alerta.Tipo == tipo && alerta.CondicaoAtiva)]);

        public Task<AlertaEntity?> ObterAsync(Guid alertaId, CancellationToken cancellationToken) =>
            Task.FromResult(Linhas.FirstOrDefault(alerta => alerta.Id == alertaId));

        public Task GravarAsync(AlertaEntity alerta, CancellationToken cancellationToken)
        {
            // Espelha o índice parcial ux_alerta_condicao_aberta: duplicidade de condição
            // aberta é impossível no banco, e o dublê não pode ser mais permissivo que ele.
            if (!Linhas.Any(existente =>
                    existente.ChaveCondicao == alerta.ChaveCondicao
                    && existente.Estado == EstadoAlerta.Aberto))
            {
                Linhas.Add(alerta);
            }

            return Task.CompletedTask;
        }

        public Task<bool> AtualizarEstadoAsync(
            AlertaEntity alerta, CancellationToken cancellationToken) =>
            Task.FromResult(Linhas.Contains(alerta));

        public IReadOnlyList<AlertaEntity> Abertos =>
            [.. Linhas.Where(alerta => alerta.Visivel)];
    }

    private static OrcamentoMensalEntity Orcado(Guid categoria, decimal valor) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Tenant,
        CategoriaId = categoria,
        Competencia = Agosto,
        ValorPlanejado = valor,
        CriadoEm = Agora,
    };

    private static LancamentoApuradoEntity Saida(decimal valor, DateOnly caixa) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Tenant,
        Descricao = "Saída",
        Valor = valor,
        ValorApurado = valor,
        DataCompetencia = caixa,
        DataCaixa = caixa,
        CriadoEm = Agora,
    };

    private static FonteRendaEntity Renda(decimal valor, int dia) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Tenant,
        Nome = "Salário",
        Valor = valor,
        DiaRecebimento = dia,
        VigenciaInicio = new DateOnly(2026, 1, 1),
        CriadoEm = Agora,
    };

    private static AlertaService Servico(
        AlertasEmMemoria alertas,
        IReadOnlyList<TotalPorCategoria>? totais = null,
        IReadOnlyList<OrcamentoMensalEntity>? orcamentos = null,
        IReadOnlyList<LancamentoApuradoEntity>? saidas = null,
        IReadOnlyList<FonteRendaEntity>? rendas = null,
        IContextoRequisicao? contexto = null)
    {
        var relatorios = new Mock<IRelatorioReadRepository>();
        relatorios.Setup(r => r.TotaisPorCategoriaAsync(
                It.IsAny<PeriodoDoRelatorio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(totais ?? []);
        relatorios.Setup(r => r.ExtratoAsync(
                It.IsAny<PeriodoDoRelatorio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saidas ?? []);

        var orcamentoRepo = new Mock<IOrcamentoReadRepository>();
        orcamentoRepo.Setup(r => r.ListarDoMesAsync(
                It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamentos ?? []);

        var categorias = new Mock<ICategoriaReadRepository>();
        categorias.Setup(r => r.ListarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new CategoriaEntity { Id = Transporte, TenantId = Tenant, Nome = "Transporte" },
                new CategoriaEntity { Id = Alimentacao, TenantId = Tenant, Nome = "Alimentação" },
            ]);

        var fontes = new Mock<IFonteRendaReadRepository>();
        fontes.Setup(r => r.ListarAsync(It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rendas ?? []);

        return new AlertaService(
            alertas,
            relatorios.Object,
            orcamentoRepo.Object,
            categorias.Object,
            fontes.Object,
            contexto ?? new ContextoDeTeste(),
            new RelogioFixo());
    }

    // ---------------------------------------------------------------- competência

    /// <summary>Cenário literal da spec: 400 planejados, 420 realizados.</summary>
    [Fact]
    public async Task Orcamento_excedido_emite_alerta_com_categoria_mes_e_valor()
    {
        var alertas = new AlertasEmMemoria();

        var resultado = await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 420m, 3)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        Assert.Equal(1, resultado.Emitidos);

        var alerta = Assert.Single(alertas.Abertos);

        Assert.Equal(TipoAlerta.Competencia, alerta.Tipo);
        Assert.Contains("Transporte", alerta.Mensagem, StringComparison.Ordinal);
        Assert.Contains("08/2026", alerta.Mensagem, StringComparison.Ordinal);
        Assert.Contains("20,00", alerta.Mensagem, StringComparison.Ordinal);
    }

    /// <summary>
    /// Sem orçamento não há do que estourar. Alertar aqui transformaria "ainda não planejei
    /// isso" em problema, e o produto passaria a repreender quem está começando.
    /// </summary>
    [Fact]
    public async Task Categoria_sem_orcamento_nao_gera_alerta()
    {
        var alertas = new AlertasEmMemoria();

        var resultado = await Servico(
                alertas,
                totais: [new TotalPorCategoria(Alimentacao, 5000m, 10)],
                orcamentos: [])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        Assert.Equal(0, resultado.Emitidos);
        Assert.Empty(alertas.Linhas);
    }

    [Fact]
    public async Task Gasto_exatamente_no_planejado_nao_gera_alerta()
    {
        var alertas = new AlertasEmMemoria();

        await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 400m, 2)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        Assert.Empty(alertas.Abertos);
    }

    /// <summary>
    /// A pessoa remove ou reclassifica lançamentos e a categoria volta para dentro do plano.
    /// </summary>
    [Fact]
    public async Task Estouro_corrigido_encerra_o_alerta()
    {
        var alertas = new AlertasEmMemoria();

        await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 420m, 3)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        Assert.Single(alertas.Abertos);

        var resultado = await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 380m, 2)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        Assert.Equal(1, resultado.Encerrados);
        Assert.Empty(alertas.Abertos);
        Assert.Equal(EstadoAlerta.Encerrado, alertas.Linhas[0].Estado);
        Assert.Equal(Agora, alertas.Linhas[0].EncerradoEm);
    }

    /// <summary>
    /// Reavaliar com o mesmo quadro não pode gerar um segundo aviso — senão o alerta voltaria a
    /// cada lançamento gravado e a pessoa aprenderia a ignorá-lo.
    /// </summary>
    [Fact]
    public async Task Mesma_condicao_nao_emite_alerta_duas_vezes()
    {
        var alertas = new AlertasEmMemoria();

        for (var vez = 0; vez < 3; vez++)
        {
            var resultado = await Servico(
                    alertas,
                    totais: [new TotalPorCategoria(Transporte, 420m, 3)],
                    orcamentos: [Orcado(Transporte, 400m)])
                .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

            Assert.Equal(vez == 0 ? 1 : 0, resultado.Emitidos);
        }

        Assert.Single(alertas.Linhas);
    }

    /// <summary>
    /// Dispensado continua bloqueando: "não me avise disso" tem de durar enquanto a condição
    /// existir, e não até o próximo lançamento.
    /// </summary>
    [Fact]
    public async Task Alerta_dispensado_nao_e_reemitido_enquanto_a_condicao_valer()
    {
        var alertas = new AlertasEmMemoria();

        await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 420m, 3)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        alertas.Linhas[0].Dispensar(Agora);

        var resultado = await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 500m, 4)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        Assert.Equal(0, resultado.Emitidos);
        Assert.Equal(1, resultado.JaConhecidos);
        Assert.Empty(alertas.Abertos);
    }

    /// <summary>
    /// Encerrar o dispensado é o que permite o aviso voltar quando o problema reaparece. Sem
    /// isso, uma dispensa de agosto silenciaria a mesma categoria para sempre.
    /// </summary>
    [Fact]
    public async Task Condicao_encerrada_e_repetida_volta_a_alertar()
    {
        var alertas = new AlertasEmMemoria();

        await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 420m, 3)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        alertas.Linhas[0].Dispensar(Agora);

        await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 100m, 1)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        Assert.Equal(EstadoAlerta.Encerrado, alertas.Linhas[0].Estado);

        var resultado = await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 900m, 6)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        Assert.Equal(1, resultado.Emitidos);
        Assert.Single(alertas.Abertos);
    }

    /// <summary>
    /// O tenant recebido por compartilhamento é somente leitura: o alerta é do dono, e gravar
    /// aqui seria escrever no espaço alheio.
    /// </summary>
    [Fact]
    public async Task Contexto_somente_leitura_nao_avalia_nem_grava()
    {
        var alertas = new AlertasEmMemoria();

        var resultado = await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 420m, 3)],
                orcamentos: [Orcado(Transporte, 400m)],
                contexto: new ContextoDeTeste { SomenteLeitura = true })
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        Assert.Equal(ResultadoDaAvaliacao.Nenhum, resultado);
        Assert.Empty(alertas.Linhas);
    }

    [Fact]
    public async Task Sem_tenant_selecionado_nao_avalia()
    {
        var alertas = new AlertasEmMemoria();

        var resultado = await Servico(
                alertas,
                contexto: new ContextoDeTeste { TenantId = null })
            .AvaliarCaixaAsync(CancellationToken.None);

        Assert.Equal(ResultadoDaAvaliacao.Nenhum, resultado);
    }

    // ---------------------------------------------------------------- caixa

    /// <summary>
    /// Cenário literal da spec: saídas de 3.400 até 10/08 contra renda de 2.100 disponível,
    /// faltando 1.300.
    /// </summary>
    [Fact]
    public async Task Saldo_projetado_insuficiente_emite_alerta_com_data_e_diferenca()
    {
        var alertas = new AlertasEmMemoria();

        var resultado = await Servico(
                alertas,
                saidas: [Saida(3400m, new DateOnly(2026, 8, 10))],
                rendas: [Renda(2100m, 5)])
            .AvaliarCaixaAsync(CancellationToken.None);

        Assert.Equal(1, resultado.Emitidos);

        var alerta = Assert.Single(alertas.Abertos);

        Assert.Equal(TipoAlerta.Caixa, alerta.Tipo);
        Assert.Contains("10/08/2026", alerta.Mensagem, StringComparison.Ordinal);
        Assert.Contains("1.300,00", alerta.Mensagem, StringComparison.Ordinal);
    }

    /// <summary>
    /// O alerta sai hoje para um problema de daqui a seis dias. É o ponto do produto: avisar
    /// no dia da falta serviria só para confirmar o que já aconteceu.
    /// </summary>
    [Fact]
    public async Task Alerta_de_caixa_e_emitido_antes_da_data_projetada()
    {
        var alertas = new AlertasEmMemoria();

        await Servico(
                alertas,
                saidas: [Saida(3400m, new DateOnly(2026, 8, 10))],
                rendas: [Renda(2100m, 5)])
            .AvaliarCaixaAsync(CancellationToken.None);

        var alerta = Assert.Single(alertas.Abertos);

        Assert.Equal(Agora, alerta.CriadoEm);
        Assert.True(DateOnly.FromDateTime(alerta.CriadoEm.UtcDateTime) < new DateOnly(2026, 8, 10));
    }

    [Fact]
    public async Task Renda_que_cobre_as_saidas_nao_gera_alerta()
    {
        var alertas = new AlertasEmMemoria();

        var resultado = await Servico(
                alertas,
                saidas: [Saida(1000m, new DateOnly(2026, 8, 10))],
                rendas: [Renda(6000m, 5)])
            .AvaliarCaixaAsync(CancellationToken.None);

        Assert.Equal(0, resultado.Emitidos);
        Assert.Empty(alertas.Linhas);
    }

    [Fact]
    public async Task Insuficiencia_resolvida_encerra_o_alerta_de_caixa()
    {
        var alertas = new AlertasEmMemoria();

        await Servico(
                alertas,
                saidas: [Saida(3400m, new DateOnly(2026, 8, 10))],
                rendas: [Renda(2100m, 5)])
            .AvaliarCaixaAsync(CancellationToken.None);

        Assert.Single(alertas.Abertos);

        var resultado = await Servico(
                alertas,
                saidas: [Saida(500m, new DateOnly(2026, 8, 10))],
                rendas: [Renda(2100m, 5)])
            .AvaliarCaixaAsync(CancellationToken.None);

        Assert.Equal(1, resultado.Encerrados);
        Assert.Empty(alertas.Abertos);
    }

    // ---------------------------------------------------------------- os dois juntos

    /// <summary>
    /// Estourar o plano e ficar sem dinheiro no dia certo são problemas diferentes, com ações
    /// diferentes. Um aviso combinado esconderia qual das duas coisas resolver.
    /// </summary>
    [Fact]
    public async Task Estouro_e_insuficiencia_simultaneos_produzem_dois_alertas_distintos()
    {
        var alertas = new AlertasEmMemoria();

        var servico = Servico(
            alertas,
            totais: [new TotalPorCategoria(Transporte, 420m, 3)],
            orcamentos: [Orcado(Transporte, 400m)],
            saidas: [Saida(3400m, new DateOnly(2026, 8, 10))],
            rendas: [Renda(2100m, 5)]);

        await servico.AvaliarCompetenciaAsync(Agosto, CancellationToken.None);
        await servico.AvaliarCaixaAsync(CancellationToken.None);

        Assert.Equal(2, alertas.Abertos.Count);
        Assert.Contains(alertas.Abertos, alerta => alerta.Tipo == TipoAlerta.Competencia);
        Assert.Contains(alertas.Abertos, alerta => alerta.Tipo == TipoAlerta.Caixa);
        Assert.Equal(2, alertas.Abertos.Select(alerta => alerta.Mensagem).Distinct().Count());
    }

    /// <summary>
    /// Encerrar um não pode encerrar o outro: as duas conciliações só tocam as chaves do seu
    /// próprio tipo.
    /// </summary>
    [Fact]
    public async Task Resolver_o_caixa_nao_encerra_o_alerta_de_competencia()
    {
        var alertas = new AlertasEmMemoria();

        await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 420m, 3)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        await Servico(
                alertas,
                saidas: [Saida(3400m, new DateOnly(2026, 8, 10))],
                rendas: [Renda(2100m, 5)])
            .AvaliarCaixaAsync(CancellationToken.None);

        await Servico(alertas, rendas: [Renda(6000m, 5)])
            .AvaliarCaixaAsync(CancellationToken.None);

        var restante = Assert.Single(alertas.Abertos);

        Assert.Equal(TipoAlerta.Competencia, restante.Tipo);
    }

    // ---------------------------------------------------------------- projeção

    [Fact]
    public void Projecao_acumula_entradas_e_saidas_por_data()
    {
        var pontos = ProjecaoDeCaixa.Projetar(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            [Renda(6000m, 5)],
            [
                new SaidaPrevista(new DateOnly(2026, 8, 3), 1000m),
                new SaidaPrevista(new DateOnly(2026, 8, 10), 2000m),
            ]);

        Assert.Equal(3, pontos.Count);
        Assert.Equal(-1000m, pontos[0].Saldo);
        Assert.Equal(5000m, pontos[1].Saldo);
        Assert.Equal(3000m, pontos[2].Saldo);
    }

    /// <summary>A primeira, e não a pior: é ali que ainda dá tempo de agir.</summary>
    [Fact]
    public void Primeira_insuficiencia_e_a_primeira_data_negativa()
    {
        var pontos = ProjecaoDeCaixa.Projetar(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            [],
            [
                new SaidaPrevista(new DateOnly(2026, 8, 3), 100m),
                new SaidaPrevista(new DateOnly(2026, 8, 20), 900m),
            ]);

        var falta = ProjecaoDeCaixa.PrimeiraInsuficiencia(pontos);

        Assert.NotNull(falta);
        Assert.Equal(new DateOnly(2026, 8, 3), falta.Data);
        Assert.Equal(100m, falta.Diferenca);
    }

    [Fact]
    public void Projecao_sem_insuficiencia_devolve_nulo() =>
        Assert.Null(ProjecaoDeCaixa.PrimeiraInsuficiencia(
            ProjecaoDeCaixa.Projetar(
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 31),
                [Renda(6000m, 1)],
                [new SaidaPrevista(new DateOnly(2026, 8, 10), 100m)])));

    /// <summary>
    /// Renda no dia 31 numa janela que cruza fevereiro precisa cair no dia 28 — se sumisse, a
    /// projeção acusaria falta que não existe.
    /// </summary>
    [Fact]
    public void Renda_no_dia_31_entra_em_fevereiro_no_ultimo_dia()
    {
        var pontos = ProjecaoDeCaixa.Projetar(
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 2, 28),
            [Renda(6000m, 31)],
            []);

        var ponto = Assert.Single(pontos);

        Assert.Equal(new DateOnly(2026, 2, 28), ponto.Data);
        Assert.Equal(6000m, ponto.Entradas);
    }

    [Fact]
    public void Saida_fora_da_janela_nao_entra_na_projecao()
    {
        var pontos = ProjecaoDeCaixa.Projetar(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            [],
            [new SaidaPrevista(new DateOnly(2026, 9, 5), 5000m)]);

        Assert.Empty(pontos);
    }

    // ---------------------------------------------------------------- chave e entidade

    [Fact]
    public void Chave_de_orcamento_identifica_categoria_e_mes()
    {
        var agosto = ChaveDeCondicao.OrcamentoEstourado(Transporte, Agosto);
        var setembro = ChaveDeCondicao.OrcamentoEstourado(Transporte, new DateOnly(2026, 9, 1));

        Assert.NotEqual(agosto, setembro);
        Assert.EndsWith("2026-08", agosto, StringComparison.Ordinal);
    }

    /// <summary>
    /// Duas datas insuficientes no mesmo mês são dois problemas: pagar o dia 5 pode ser
    /// possível e o dia 20 não.
    /// </summary>
    [Fact]
    public void Chave_de_caixa_e_por_data_e_nao_por_mes() =>
        Assert.NotEqual(
            ChaveDeCondicao.SaldoInsuficiente(new DateOnly(2026, 8, 5)),
            ChaveDeCondicao.SaldoInsuficiente(new DateOnly(2026, 8, 20)));

    [Fact]
    public void Alerta_encerrado_deixa_de_ter_condicao_ativa()
    {
        var alerta = new AlertaEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = Tenant,
            Tipo = TipoAlerta.Caixa,
            ChaveCondicao = "caixa:2026-08-10",
            Mensagem = "x",
            CriadoEm = Agora,
        };

        Assert.True(alerta.Visivel);
        Assert.True(alerta.CondicaoAtiva);

        alerta.Dispensar(Agora);

        Assert.False(alerta.Visivel);
        Assert.True(alerta.CondicaoAtiva);
        Assert.Equal(Agora, alerta.DispensadoEm);

        alerta.Encerrar(Agora);

        Assert.False(alerta.CondicaoAtiva);
    }

    // ---------------------------------------------------------------- dispensa

    [Fact]
    public async Task Dispensa_marca_o_alerta_e_guarda_a_data()
    {
        var alertas = new AlertasEmMemoria();

        await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 420m, 3)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        var alerta = alertas.Linhas[0];

        var resultado = await new DispensarAlertaCommandHandler(alertas, new RelogioFixo())
            .HandleAsync(new DispensarAlertaCommand(Tenant, alerta.Id), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(EstadoAlerta.Dispensado, alerta.Estado);
        Assert.Equal(Agora, alerta.DispensadoEm);
    }

    [Fact]
    public async Task Dispensa_de_alerta_inexistente_e_recusada()
    {
        var resultado = await new DispensarAlertaCommandHandler(
                new AlertasEmMemoria(), new RelogioFixo())
            .HandleAsync(
                new DispensarAlertaCommand(Tenant, Guid.CreateVersion7()), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            DispensarAlertaCommandHandler.CodigoAlertaNaoEncontrado, resultado.Erros[0].Codigo);
    }

    /// <summary>
    /// Dispensar duas vezes moveria a data de dispensa e apagaria quando a pessoa de fato
    /// decidiu.
    /// </summary>
    [Fact]
    public async Task Alerta_ja_dispensado_nao_se_dispensa_de_novo()
    {
        var alertas = new AlertasEmMemoria();

        await Servico(
                alertas,
                totais: [new TotalPorCategoria(Transporte, 420m, 3)],
                orcamentos: [Orcado(Transporte, 400m)])
            .AvaliarCompetenciaAsync(Agosto, CancellationToken.None);

        alertas.Linhas[0].Dispensar(Agora);

        var resultado = await new DispensarAlertaCommandHandler(alertas, new RelogioFixo())
            .HandleAsync(
                new DispensarAlertaCommand(Tenant, alertas.Linhas[0].Id), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            DispensarAlertaCommandHandler.CodigoAlertaNaoDispensavel, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Dispensa_sem_identificar_o_alerta_e_recusada()
    {
        var resultado = await new DispensarAlertaCommandHandler(
                new AlertasEmMemoria(), new RelogioFixo())
            .HandleAsync(new DispensarAlertaCommand(Tenant, Guid.Empty), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            DispensarAlertaCommandValidation.CodigoAlertaObrigatorio, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Dispensa_que_nao_encontra_a_linha_ao_gravar_e_recusada()
    {
        var alertas = new Mock<IAlertaWriteRepository>();
        var alerta = new AlertaEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = Tenant,
            Tipo = TipoAlerta.Caixa,
            ChaveCondicao = "caixa:2026-08-10",
            Mensagem = "x",
            CriadoEm = Agora,
        };

        alertas.Setup(r => r.ObterAsync(alerta.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerta);
        alertas.Setup(r => r.AtualizarEstadoAsync(
                It.IsAny<AlertaEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await new DispensarAlertaCommandHandler(alertas.Object, new RelogioFixo())
            .HandleAsync(new DispensarAlertaCommand(Tenant, alerta.Id), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            DispensarAlertaCommandHandler.CodigoAlertaNaoEncontrado, resultado.Erros[0].Codigo);
    }

    // ---------------------------------------------------------------- entrega

    [Fact]
    public async Task Dono_do_tenant_recebe_os_alertas()
    {
        var repositorio = new Mock<IAlertaReadRepository>();
        repositorio.Setup(r => r.ListarAbertosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AlertaEntity
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = Tenant,
                    Tipo = TipoAlerta.Competencia,
                    ChaveCondicao = "orcamento:x:2026-08",
                    Mensagem = "Transporte passou do orçamento.",
                    CriadoEm = Agora,
                },
            ]);

        var resultado = await new AlertasAbertosQueryHandler(repositorio.Object).HandleAsync(
            new AlertasAbertosQuery(Tenant, SomenteLeitura: false), CancellationToken.None);

        var alerta = Assert.Single(resultado.Alertas);

        Assert.Equal(TipoAlerta.Competencia, alerta.Tipo);
    }

    /// <summary>
    /// Alerta não é informação de acompanhamento, é convocação para agir — e quem age é o dono.
    /// A RLS deixaria o convidado ler a tabela; a recusa é do produto.
    /// </summary>
    [Fact]
    public async Task Quem_tem_somente_leitura_nao_recebe_alerta()
    {
        var repositorio = new Mock<IAlertaReadRepository>();

        var resultado = await new AlertasAbertosQueryHandler(repositorio.Object).HandleAsync(
            new AlertasAbertosQuery(Tenant, SomenteLeitura: true), CancellationToken.None);

        Assert.Empty(resultado.Alertas);

        repositorio.Verify(r => r.ListarAbertosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consulta_de_alertas_sem_tenant_devolve_vazio()
    {
        var repositorio = new Mock<IAlertaReadRepository>();

        var resultado = await new AlertasAbertosQueryHandler(repositorio.Object).HandleAsync(
            new AlertasAbertosQuery(Guid.Empty, SomenteLeitura: false), CancellationToken.None);

        Assert.Empty(resultado.Alertas);
    }

    // ---------------------------------------------------------------- rotina diária

    [Fact]
    public async Task Rotina_percorre_todos_os_tenants_assumindo_cada_um()
    {
        var primeiro = new TenantParaRotina(Guid.CreateVersion7(), Guid.CreateVersion7());
        var segundo = new TenantParaRotina(Guid.CreateVersion7(), Guid.CreateVersion7());

        var tenants = new Mock<IRotinaReadRepository>();
        tenants.Setup(r => r.TenantsAtivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([primeiro, segundo]);

        var assumidos = new List<Guid>();
        var contexto = new Mock<IContextoAssumivel>();
        contexto.Setup(c => c.Assumir(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Callback<Guid, Guid>((_, tenantId) => assumidos.Add(tenantId));

        var servico = new Mock<IAlertaService>();
        servico.Setup(s => s.AvaliarCaixaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoDaAvaliacao(1, 0, 0));

        var resultado = await new RotinaDeAlertasDeCaixa(
                tenants.Object,
                contexto.Object,
                servico.Object,
                NullLogger<RotinaDeAlertasDeCaixa>.Instance)
            .ExecutarAsync(CancellationToken.None);

        Assert.Equal(2, resultado.TenantsAvaliados);
        Assert.Equal(2, resultado.Emitidos);
        Assert.Equal(0, resultado.Falhas);
        Assert.Equal([primeiro.TenantId, segundo.TenantId], assumidos);
    }

    /// <summary>
    /// A rotina roda uma vez por dia: abortar no primeiro erro deixaria toda a base sem alerta
    /// até alguém perceber.
    /// </summary>
    [Fact]
    public async Task Falha_em_um_tenant_nao_impede_os_demais()
    {
        var quebrado = new TenantParaRotina(Guid.CreateVersion7(), Guid.CreateVersion7());
        var saudavel = new TenantParaRotina(Guid.CreateVersion7(), Guid.CreateVersion7());

        var tenants = new Mock<IRotinaReadRepository>();
        tenants.Setup(r => r.TenantsAtivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([quebrado, saudavel]);

        var atual = Guid.Empty;
        var contexto = new Mock<IContextoAssumivel>();
        contexto.Setup(c => c.Assumir(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Callback<Guid, Guid>((_, tenantId) => atual = tenantId);

        var servico = new Mock<IAlertaService>();
        servico.Setup(s => s.AvaliarCaixaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => atual == quebrado.TenantId
                ? throw new InvalidOperationException("dado inconsistente")
                : new ResultadoDaAvaliacao(0, 1, 0));

        var resultado = await new RotinaDeAlertasDeCaixa(
                tenants.Object,
                contexto.Object,
                servico.Object,
                NullLogger<RotinaDeAlertasDeCaixa>.Instance)
            .ExecutarAsync(CancellationToken.None);

        Assert.Equal(1, resultado.Falhas);
        Assert.Equal(1, resultado.TenantsAvaliados);
        Assert.Equal(1, resultado.Encerrados);
    }
}
