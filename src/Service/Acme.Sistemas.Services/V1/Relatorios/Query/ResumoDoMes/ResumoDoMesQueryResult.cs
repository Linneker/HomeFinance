using Acme.Sistemas.Domain.Entities.Relatorios;

namespace Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;

/// <param name="Categorias">
/// Uma linha por categoria com orçamento definido, inclusive as sem gasto — o que foi planejado
/// e não usado é metade da informação de um orçamento.
/// </param>
/// <param name="Diferenca">
/// Planejado menos realizado no mês inteiro. Negativo significa que o mês passou do plano.
/// </param>
public sealed record ResumoDoMesQueryResult(
    DateOnly Competencia,
    bool VisaoPorCaixa,
    IReadOnlyList<LinhaDoResumo> Categorias,
    ForaDoPlanejamento ForaDoPlanejamento,
    decimal TotalPlanejado,
    decimal TotalRealizado)
{
    public decimal Diferenca => TotalPlanejado - TotalRealizado;

    public static ResumoDoMesQueryResult Vazio(DateOnly competencia, bool porCaixa) =>
        new(competencia, porCaixa, [], new ForaDoPlanejamento([], 0m, 0m), 0m, 0m);
}
