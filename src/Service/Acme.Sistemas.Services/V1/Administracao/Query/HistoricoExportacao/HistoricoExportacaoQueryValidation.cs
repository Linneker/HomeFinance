using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Administracao.Query.HistoricoExportacao;

public static class HistoricoExportacaoQueryValidation
{
    public const string CodigoContaObrigatoria = "conta_obrigatoria";

    public static IReadOnlyList<Error> Validar(HistoricoExportacaoQuery query) =>
        query.ContaUsuarioId == Guid.Empty
            ? [new Error(CodigoContaObrigatoria, "Conta não identificada.", nameof(query.ContaUsuarioId))]
            : [];
}
