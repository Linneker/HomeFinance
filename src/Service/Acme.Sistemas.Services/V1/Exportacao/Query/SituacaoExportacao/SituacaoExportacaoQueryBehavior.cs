using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Exportacao.Query.SituacaoExportacao;

public sealed class SituacaoExportacaoQueryBehavior
    : IPipelineBehavior<SituacaoExportacaoQuery, SituacaoExportacaoQueryResult>
{
    public Task<SituacaoExportacaoQueryResult> HandleAsync(
        SituacaoExportacaoQuery request,
        RequestHandlerDelegate<SituacaoExportacaoQueryResult> next,
        CancellationToken cancellationToken) => next();
}
