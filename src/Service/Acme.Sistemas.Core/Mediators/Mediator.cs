using System.Collections.Concurrent;
using System.Reflection;
using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Mediators.Notification;
using Microsoft.Extensions.DependencyInjection;

namespace Acme.Sistemas.Core.Mediators;

/// <summary>
/// Implementação do <see cref="IMediator"/>. Resolve handler e behaviors pelo contêiner de DI
/// e monta o pipeline sob demanda, com os wrappers genéricos em cache por tipo de request.
/// </summary>
public sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> RequestWrappers = new();
    private static readonly ConcurrentDictionary<Type, Type> NotificationHandlerTypes = new();

    public Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerWrapper<TResponse>)RequestWrappers.GetOrAdd(
            request.GetType(),
            static requestType =>
            {
                var wrapperType = typeof(RequestHandlerWrapperImpl<,>)
                    .MakeGenericType(requestType, typeof(TResponse));

                return Activator.CreateInstance(wrapperType)
                       ?? throw new InvalidOperationException(
                           $"Não foi possível criar o wrapper de pipeline para '{requestType.FullName}'.");
            });

        return wrapper.HandleAsync(request, serviceProvider, cancellationToken);
    }

    public async Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        // O tipo em tempo de execução pode ser mais derivado que TNotification — resolver por ele
        // garante que um evento publicado como interface base chegue aos handlers corretos.
        var handlerType = NotificationHandlerTypes.GetOrAdd(
            notification.GetType(),
            static notificationType => typeof(INotificationHandler<>).MakeGenericType(notificationType));

        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            var method = handlerType.GetMethod(nameof(INotificationHandler<INotification>.HandleAsync))
                         ?? throw new InvalidOperationException(
                             $"Handler '{handler.GetType().FullName}' não expõe HandleAsync.");

            try
            {
                await (Task)method.Invoke(handler, [notification, cancellationToken])!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                // Sem isso, toda falha de handler de evento chegaria embrulhada em
                // TargetInvocationException e esconderia a exceção real no log.
                throw ex.InnerException;
            }
        }
    }

    private abstract class RequestHandlerWrapper<TResponse>
    {
        public abstract Task<TResponse> HandleAsync(
            IRequest<TResponse> request,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken);
    }

    private sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> HandleAsync(
            IRequest<TResponse> request,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

            RequestHandlerDelegate<TResponse> pipeline =
                () => handler.HandleAsync(typedRequest, cancellationToken);

            var behaviors = serviceProvider
                .GetServices<IPipelineBehavior<TRequest, TResponse>>()
                .Reverse();

            // Envolver na ordem inversa faz o primeiro behavior registrado virar o mais externo.
            foreach (var behavior in behaviors)
            {
                var next = pipeline;
                pipeline = () => behavior.HandleAsync(typedRequest, next, cancellationToken);
            }

            return pipeline();
        }
    }
}
