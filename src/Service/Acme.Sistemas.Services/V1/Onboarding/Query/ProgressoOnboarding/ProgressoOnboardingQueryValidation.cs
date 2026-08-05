using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Onboarding.Query.ProgressoOnboarding;

public static class ProgressoOnboardingQueryValidation
{
    public const string CodigoTenantObrigatorio = "tenant_obrigatorio";

    public static IReadOnlyList<Error> Validar(ProgressoOnboardingQuery query) =>
        query.TenantId == Guid.Empty
            ? [new Error(CodigoTenantObrigatorio, "Nenhum espaço selecionado.")]
            : [];
}
