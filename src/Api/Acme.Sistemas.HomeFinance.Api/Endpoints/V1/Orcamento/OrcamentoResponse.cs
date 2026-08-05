namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Orcamento;

public sealed record OrcamentoItemResponse(Guid Id, Guid CategoriaId, decimal ValorPlanejado);

/// <param name="Aviso">
/// Planejamento acima da renda prevista. Nulo quando não há excesso. Não é erro: a gravação
/// aconteceu.
/// </param>
public sealed record OrcamentoDoMesResponse(
    DateOnly Competencia,
    IReadOnlyList<OrcamentoItemResponse> Categorias,
    decimal TotalPlanejado,
    decimal RendaPrevista,
    string? Aviso);

public sealed record OrcamentoDefinidoResponse(
    Guid Id,
    DateOnly Competencia,
    decimal TotalPlanejado,
    decimal RendaPrevista,
    string? Aviso);

public sealed record OrcamentoCopiadoResponse(
    DateOnly Destino,
    int LinhasCopiadas,
    decimal TotalPlanejado,
    decimal RendaPrevista,
    string? Aviso);
