using Acme.Sistemas.Domain.Entities.Planejamento;

namespace Acme.Sistemas.Domain.Entities.Alertas;

/// <param name="Saldo">Acumulado da janela até esta data, entradas menos saídas.</param>
public sealed record PontoDaProjecao(
    DateOnly Data,
    decimal Entradas,
    decimal Saidas,
    decimal Saldo);

/// <param name="Diferenca">Quanto falta na data. Sempre positivo.</param>
public sealed record InsuficienciaProjetada(DateOnly Data, decimal Diferenca);

/// <summary>Saída de caixa prevista numa data.</summary>
public sealed record SaidaPrevista(DateOnly Data, decimal Valor);

/// <summary>
/// Projeta o saldo dia a dia a partir da renda prevista e das saídas de caixa.
/// </summary>
/// <remarks>
/// Parte de zero, e não do saldo bancário: não há integração com banco nesta fase. A projeção
/// responde, portanto, "o que entra nesta janela cobre o que sai nela?" — que é a pergunta que
/// o produto consegue responder honestamente com o que a pessoa declarou. Fingir um saldo
/// inicial faria o alerta disparar ou silenciar por um número inventado.
/// <para>
/// Só as datas em que algo acontece viram ponto. Percorrer todos os dias da janela daria a
/// mesma resposta e cresceria com o horizonte, não com o movimento.
/// </para>
/// </remarks>
public static class ProjecaoDeCaixa
{
    public static IReadOnlyList<PontoDaProjecao> Projetar(
        DateOnly de,
        DateOnly ate,
        IEnumerable<FonteRendaEntity> fontes,
        IEnumerable<SaidaPrevista> saidas)
    {
        var entradas = EntradasDaJanela(de, ate, fontes);

        var movimentos = entradas
            .Select(entrada => (entrada.Data, Entrada: entrada.Valor, Saida: 0m))
            .Concat(saidas
                .Where(saida => saida.Data >= de && saida.Data <= ate)
                .Select(saida => (saida.Data, Entrada: 0m, Saida: saida.Valor)))
            .GroupBy(movimento => movimento.Data)
            .OrderBy(grupo => grupo.Key);

        var pontos = new List<PontoDaProjecao>();
        var saldo = 0m;

        foreach (var dia in movimentos)
        {
            var recebido = dia.Sum(movimento => movimento.Entrada);
            var pago = dia.Sum(movimento => movimento.Saida);

            saldo += recebido - pago;

            pontos.Add(new PontoDaProjecao(dia.Key, recebido, pago, saldo));
        }

        return pontos;
    }

    /// <summary>
    /// Primeira data da janela em que o saldo projetado fica negativo, se houver.
    /// </summary>
    /// <remarks>
    /// A primeira, e não a pior: é ali que a pessoa ainda tem tempo de agir. Avisar sobre a
    /// maior falta do mês serviria para o susto, não para a decisão.
    /// </remarks>
    public static InsuficienciaProjetada? PrimeiraInsuficiencia(
        IEnumerable<PontoDaProjecao> pontos)
    {
        var falha = pontos.FirstOrDefault(ponto => ponto.Saldo < 0);

        return falha is null ? null : new InsuficienciaProjetada(falha.Data, -falha.Saldo);
    }

    /// <summary>
    /// Datas de recebimento das fontes vigentes dentro da janela.
    /// </summary>
    /// <remarks>
    /// Uma entrada por mês tocado pela janela, com o dia ajustado a meses curtos — a mesma
    /// regra do calendário financeiro, para a renda não desaparecer em fevereiro.
    /// </remarks>
    private static IEnumerable<(DateOnly Data, decimal Valor)> EntradasDaJanela(
        DateOnly de,
        DateOnly ate,
        IEnumerable<FonteRendaEntity> fontes)
    {
        var meses = Lancamentos.CalendarioFinanceiro.PrimeiroDiaDoMes(de);
        var ultimo = Lancamentos.CalendarioFinanceiro.PrimeiroDiaDoMes(ate);
        var lista = fontes.ToList();

        for (var mes = meses; mes <= ultimo; mes = mes.AddMonths(1))
        {
            foreach (var fonte in lista.Where(fonte => fonte.VigenteEm(mes)))
            {
                var data = fonte.RecebimentoEm(mes);

                if (data >= de && data <= ate)
                {
                    yield return (data, fonte.Valor);
                }
            }
        }
    }
}
