using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Alertas.Query.AlertasAbertos;

public static class AlertasAbertosQueryValidation
{
    public const string CodigoTenantObrigatorio = "tenant_obrigatorio";

    public static IReadOnlyList<Error> Validar(AlertasAbertosQuery query) =>
        query.TenantId == Guid.Empty
            ? [new Error(CodigoTenantObrigatorio, "Nenhum espaço selecionado.")]
            : [];
}
