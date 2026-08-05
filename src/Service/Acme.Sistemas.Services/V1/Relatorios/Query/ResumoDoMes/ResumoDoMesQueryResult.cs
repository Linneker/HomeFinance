using Acme.Sistemas.Core.Response.Erros;
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
    decimal TotalRealizado) : IResultadoRecusavel<ResumoDoMesQueryResult>
{
    public IReadOnlyList<Error> Erros { get; init; } = [];

    public decimal Diferenca => TotalPlanejado - TotalRealizado;

    public static ResumoDoMesQueryResult Vazio(DateOnly competencia, bool porCaixa) =>
        new(competencia, porCaixa, [], new ForaDoPlanejamento([], 0m, 0m), 0m, 0m);

    /// <summary>
    /// Recusa sem competência: o mês nem chegou a ser lido. A tela distingue "não há dados"
    /// de "não posso te mostrar" pelo erro, não por um resumo zerado que pareceria real.
    /// </summary>
    public static ResumoDoMesQueryResult Recusar(Error erro) =>
        Vazio(default, false) with { Erros = [erro] };
}
