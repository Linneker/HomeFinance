using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Core.Mediators;

/// <summary>Continuação do pipeline: chama o próximo behavior ou, no fim, o handler.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Middleware do pipeline de requests. É onde vive o que é transversal à ação — log, cache,
/// validação complementar, enriquecimento — para o Handler ficar só com a lógica principal.
/// </summary>
/// <remarks>
/// Behaviors registrados primeiro são os mais externos: o primeiro registrado é o primeiro a
/// receber a chamada e o último a devolvê-la.
/// </remarks>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
