using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Exportacao.Query.BaixarExportacao;

public static class BaixarExportacaoQueryValidation
{
    public const string CodigoSolicitacaoObrigatoria = "solicitacao_obrigatoria";

    public static IReadOnlyList<Error> Validar(BaixarExportacaoQuery query) =>
        query.SolicitacaoId == Guid.Empty
            ? [new Error(CodigoSolicitacaoObrigatoria, "Solicitação não identificada.", nameof(query.SolicitacaoId))]
            : [];
}
