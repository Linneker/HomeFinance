using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Planejamento.Query.OrcamentoDoMes;

/// <summary>Sem cache: o orçamento é editado na própria tela e precisa refletir a última edição.</summary>
public sealed class OrcamentoDoMesQueryBehavior
    : IPipelineBehavior<OrcamentoDoMesQuery, OrcamentoDoMesQueryResult>
{
    public Task<OrcamentoDoMesQueryResult> HandleAsync(
        OrcamentoDoMesQuery request,
        RequestHandlerDelegate<OrcamentoDoMesQueryResult> next,
        CancellationToken cancellationToken) => next();
}
