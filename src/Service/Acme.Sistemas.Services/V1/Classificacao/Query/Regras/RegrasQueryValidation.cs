using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Classificacao.Query.Regras;

public static class RegrasQueryValidation
{
    public const string CodigoTenantObrigatorio = "tenant_obrigatorio";

    public static IReadOnlyList<Error> Validar(RegrasQuery query) =>
        query.TenantId == Guid.Empty
            ? [new Error(CodigoTenantObrigatorio, "Nenhum espaço selecionado.")]
            : [];
}
