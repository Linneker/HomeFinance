using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Administracao.Query.HistoricoExportacao;

public sealed class HistoricoExportacaoQueryBehavior
    : IPipelineBehavior<HistoricoExportacaoQuery, HistoricoExportacaoQueryResult>
{
    public Task<HistoricoExportacaoQueryResult> HandleAsync(
        HistoricoExportacaoQuery request,
        RequestHandlerDelegate<HistoricoExportacaoQueryResult> next,
        CancellationToken cancellationToken) => next();
}
