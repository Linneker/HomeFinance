namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.TenantsDisponiveis;

/// <param name="SomenteLeitura">
/// Verdadeiro nos tenants recebidos por compartilhamento. O app usa isto para deixar o modo
/// visível o tempo todo — quem não sabe que está lendo dado alheio tenta editar e se frustra.
/// </param>
public sealed record TenantSelecionavel(Guid TenantId, bool EhProprio, bool SomenteLeitura);

public sealed record TenantsDisponiveisQueryResult(IReadOnlyList<TenantSelecionavel> Tenants);
