using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Alertas.Query.AlertasAbertos;

/// <summary>
/// Sem cache. O alerta some assim que a pessoa dispensa ou corrige a causa, e um cache faria
/// o aviso resolvido continuar na tela — que é exatamente o comportamento que ensina a ignorar
/// alertas.
/// </summary>
public sealed class AlertasAbertosQueryBehavior
    : IPipelineBehavior<AlertasAbertosQuery, AlertasAbertosQueryResult>
{
    public Task<AlertasAbertosQueryResult> HandleAsync(
        AlertasAbertosQuery request,
        RequestHandlerDelegate<AlertasAbertosQueryResult> next,
        CancellationToken cancellationToken) => next();
}
