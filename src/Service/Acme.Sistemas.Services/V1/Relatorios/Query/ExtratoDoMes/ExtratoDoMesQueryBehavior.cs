using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Relatorios.Query.ExtratoDoMes;

/// <summary>
/// Sem cache. O extrato é do tenant e muda a cada classificação feita na própria tela — um
/// cache aqui devolveria a fila de pendentes que a pessoa acabou de esvaziar.
/// </summary>
public sealed class ExtratoDoMesQueryBehavior
    : IPipelineBehavior<ExtratoDoMesQuery, ExtratoDoMesQueryResult>
{
    public Task<ExtratoDoMesQueryResult> HandleAsync(
        ExtratoDoMesQuery request,
        RequestHandlerDelegate<ExtratoDoMesQueryResult> next,
        CancellationToken cancellationToken) => next();
}
