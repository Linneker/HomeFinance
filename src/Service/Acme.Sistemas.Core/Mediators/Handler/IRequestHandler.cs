namespace Acme.Sistemas.Core.Mediators.Handler;

/// <summary>
/// Executa a lógica principal de um <see cref="IRequest{TResponse}"/>.
/// Cada request tem exatamente um handler registrado.
/// </summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
