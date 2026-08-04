namespace Acme.Sistemas.Core.Mediators.Notification;

/// <summary>
/// Consome um evento. Um mesmo <see cref="INotification"/> pode ter vários handlers,
/// todos executados na publicação.
/// </summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
