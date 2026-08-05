using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;

public static class ResumoDoMesQueryValidation
{
    public const string CodigoTenantObrigatorio = "tenant_obrigatorio";

    public static IReadOnlyList<Error> Validar(ResumoDoMesQuery query) =>
        query.TenantId == Guid.Empty
            ? [new Error(CodigoTenantObrigatorio, "Nenhum espaço selecionado.")]
            : [];
}
