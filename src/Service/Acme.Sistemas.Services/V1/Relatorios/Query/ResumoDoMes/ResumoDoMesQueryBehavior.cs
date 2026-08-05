using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;

/// <summary>Sem cache: o resumo muda a cada lançamento e a cada ajuste do orçamento.</summary>
public sealed class ResumoDoMesQueryBehavior
    : IPipelineBehavior<ResumoDoMesQuery, ResumoDoMesQueryResult>
{
    public Task<ResumoDoMesQueryResult> HandleAsync(
        ResumoDoMesQuery request,
        RequestHandlerDelegate<ResumoDoMesQueryResult> next,
        CancellationToken cancellationToken) => next();
}
