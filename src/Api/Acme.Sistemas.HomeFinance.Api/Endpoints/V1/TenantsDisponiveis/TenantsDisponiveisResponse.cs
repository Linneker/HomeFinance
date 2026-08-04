namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.TenantsDisponiveis;

/// <param name="SomenteLeitura">
/// O app usa isto para rotular o modo de forma permanente na tela. Quem não sabe que está
/// lendo dado alheio tenta editar e se frustra.
/// </param>
public sealed record TenantDisponivelResponse(Guid TenantId, bool EhProprio, bool SomenteLeitura);

public sealed record TenantsDisponiveisResponse(IReadOnlyList<TenantDisponivelResponse> Tenants);
