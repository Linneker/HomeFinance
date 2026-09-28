using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Exportacao.Query.SituacaoExportacao;

public static class SituacaoExportacaoQueryValidation
{
    public const string CodigoSolicitacaoObrigatoria = "solicitacao_obrigatoria";

    public static IReadOnlyList<Error> Validar(SituacaoExportacaoQuery query) =>
        query.SolicitacaoId == Guid.Empty
            ? [new Error(CodigoSolicitacaoObrigatoria, "Solicitação não identificada.", nameof(query.SolicitacaoId))]
            : [];
}
