using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Mediators.Notification;

namespace Acme.Sistemas.Services.UnitTest.Fixture;

/// <summary>
/// Requests, handlers e behaviors mínimos para exercitar o pipeline do Mediator sem depender
/// de nenhuma funcionalidade de negócio.
/// </summary>
public static class MediatorFixture
{
    /// <summary>Registro compartilhado da ordem de execução observada no pipeline.</summary>
    public sealed class Rastro
    {
        public List<string> Passos { get; } = [];
    }

    public sealed record PingRequest(string Valor) : IRequest<string>;

    public sealed class PingHandler(Rastro rastro) : IRequestHandler<PingRequest, string>
    {
        public Task<string> HandleAsync(PingRequest request, CancellationToken cancellationToken)
        {
            rastro.Passos.Add("handler");
            return Task.FromResult($"pong:{request.Valor}");
        }
    }

    public sealed class BehaviorExterno(Rastro rastro)
        : IPipelineBehavior<PingRequest, string>
    {
        public async Task<string> HandleAsync(
            PingRequest request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            rastro.Passos.Add("externo:antes");
            var resultado = await next();
            rastro.Passos.Add("externo:depois");
            return resultado;
        }
    }

    public sealed class BehaviorInterno(Rastro rastro)
        : IPipelineBehavior<PingRequest, string>
    {
        public async Task<string> HandleAsync(
            PingRequest request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            rastro.Passos.Add("interno:antes");
            var resultado = await next();
            rastro.Passos.Add("interno:depois");
            return resultado;
        }
    }

    public sealed record AlgoAconteceu(string Valor) : INotification;

    public sealed class PrimeiroOuvinte(Rastro rastro) : INotificationHandler<AlgoAconteceu>
    {
        public Task HandleAsync(AlgoAconteceu notification, CancellationToken cancellationToken)
        {
            rastro.Passos.Add($"ouvinte1:{notification.Valor}");
            return Task.CompletedTask;
        }
    }

    public sealed class SegundoOuvinte(Rastro rastro) : INotificationHandler<AlgoAconteceu>
    {
        public Task HandleAsync(AlgoAconteceu notification, CancellationToken cancellationToken)
        {
            rastro.Passos.Add($"ouvinte2:{notification.Valor}");
            return Task.CompletedTask;
        }
    }

    public sealed record RequestSemHandler(string Valor) : IRequest<string>;

    public sealed record FalhaRequest : IRequest<string>;

    public sealed class FalhaHandler : IRequestHandler<FalhaRequest, string>
    {
        public Task<string> HandleAsync(FalhaRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("falha de dominio");
    }

    public sealed record EventoQueFalha : INotification;

    public sealed class OuvinteQueFalha : INotificationHandler<EventoQueFalha>
    {
        public Task HandleAsync(EventoQueFalha notification, CancellationToken cancellationToken)
            => throw new InvalidOperationException("falha no ouvinte");
    }
}
