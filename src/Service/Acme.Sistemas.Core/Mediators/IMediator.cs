using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Mediators.Notification;

namespace Acme.Sistemas.Core.Mediators;

/// <summary>
/// Ponto único de despacho de Commands, Queries e Events. Implementação própria da plataforma —
/// o blueprint Acme não usa MediatR.
/// </summary>
public interface IMediator
{
    /// <summary>Despacha um request pelo pipeline de behaviors até o handler.</summary>
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    /// <summary>Publica um evento para todos os handlers registrados.</summary>
    Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
