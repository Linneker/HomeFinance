namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Recorrencia;

public sealed record RecorrenciaCriadaResponse(Guid RecorrenciaId, int OcorrenciasGeradas);

public sealed record RecorrenciaEncerradaResponse(DateOnly APartirDe);
