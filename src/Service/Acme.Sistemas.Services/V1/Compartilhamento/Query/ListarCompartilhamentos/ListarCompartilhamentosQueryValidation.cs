using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.ListarCompartilhamentos;

public static class ListarCompartilhamentosQueryValidation
{
    public const string CodigoTenantObrigatorio = "tenant_obrigatorio";

    public static IReadOnlyList<Error> Validar(ListarCompartilhamentosQuery query) =>
        query.TenantId == Guid.Empty
            ? [new Error(CodigoTenantObrigatorio, "Espaço não identificado.")]
            : [];
}
