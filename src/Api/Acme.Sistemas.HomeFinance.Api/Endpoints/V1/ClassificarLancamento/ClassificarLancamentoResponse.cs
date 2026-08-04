namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.ClassificarLancamento;

/// <param name="PadraoDaRegra">
/// O que passou a valer para lançamentos futuros. Nulo quando a regra não foi criada — a tela
/// precisa disso para não prometer automação que não existe.
/// </param>
public sealed record ClassificarLancamentoResponse(
    DateOnly CompetenciaAfetada,
    Guid? RegraId,
    string? PadraoDaRegra);
