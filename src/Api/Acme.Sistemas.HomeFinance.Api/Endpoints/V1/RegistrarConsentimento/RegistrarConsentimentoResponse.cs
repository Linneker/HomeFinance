namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.RegistrarConsentimento;

public sealed record RegistrarConsentimentoResponse(bool Vigente, IReadOnlyList<string> Pendentes);
