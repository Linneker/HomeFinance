namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Orcamento;

public sealed record DefinirOrcamentoRequest(Guid CategoriaId, decimal ValorPlanejado);

/// <param name="Confirmado">
/// Aceite de substituir o mês de destino. Falso — o padrão — faz a operação parar e devolver
/// 409 quando o destino já tem valores.
/// </param>
public sealed record CopiarOrcamentoRequest(
    DateOnly Origem,
    DateOnly Destino,
    bool Confirmado = false);
