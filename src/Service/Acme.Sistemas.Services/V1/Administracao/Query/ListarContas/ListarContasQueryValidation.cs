using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Administracao.Query.ListarContas;

public static class ListarContasQueryValidation
{
    // A consulta não recebe parâmetros: a relação é global do back-office. Nada a validar.
    public static IReadOnlyList<Error> Validar(ListarContasQuery query) => [];
}
