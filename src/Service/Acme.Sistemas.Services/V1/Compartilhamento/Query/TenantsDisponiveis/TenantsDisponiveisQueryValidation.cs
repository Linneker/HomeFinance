using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.TenantsDisponiveis;

public static class TenantsDisponiveisQueryValidation
{
    public const string CodigoUsuarioObrigatorio = "usuario_obrigatorio";

    public static IReadOnlyList<Error> Validar(TenantsDisponiveisQuery query) =>
        query.UsuarioId == Guid.Empty
            ? [new Error(CodigoUsuarioObrigatorio, "Titular não identificado.")]
            : [];
}
