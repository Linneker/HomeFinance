using Acme.Sistemas.Services.V1.Compartilhamento.Query.TenantsDisponiveis;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.TenantsDisponiveis;

internal static class TenantsDisponiveisMap
{
    public static TenantsDisponiveisResponse ParaResponse(this TenantsDisponiveisQueryResult resultado) =>
        new([.. resultado.Tenants.Select(tenant =>
            new TenantDisponivelResponse(tenant.TenantId, tenant.EhProprio, tenant.SomenteLeitura))]);
}
