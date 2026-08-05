using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Relatorios.Query.ExtratoDoMes;

public static class ExtratoDoMesQueryValidation
{
    public const string CodigoTenantObrigatorio = "tenant_obrigatorio";

    public static IReadOnlyList<Error> Validar(ExtratoDoMesQuery query) =>
        query.TenantId == Guid.Empty
            ? [new Error(CodigoTenantObrigatorio, "Nenhum espaço selecionado.")]
            : [];
}
