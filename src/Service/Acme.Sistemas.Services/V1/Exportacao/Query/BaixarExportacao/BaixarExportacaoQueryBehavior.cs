using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Exportacao.Query.BaixarExportacao;

public sealed class BaixarExportacaoQueryBehavior
    : IPipelineBehavior<BaixarExportacaoQuery, BaixarExportacaoQueryResult>
{
    public Task<BaixarExportacaoQueryResult> HandleAsync(
        BaixarExportacaoQuery request,
        RequestHandlerDelegate<BaixarExportacaoQueryResult> next,
        CancellationToken cancellationToken) => next();
}
