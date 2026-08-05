using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Onboarding.Query.ProgressoOnboarding;

/// <summary>
/// Sem cache: a barra tem de avançar no instante em que a pessoa cumpre um requisito. Um cache
/// aqui deixaria a tela dizendo que ainda falta o que ela acabou de fazer.
/// </summary>
public sealed class ProgressoOnboardingQueryBehavior
    : IPipelineBehavior<ProgressoOnboardingQuery, ProgressoOnboardingQueryResult>
{
    public Task<ProgressoOnboardingQueryResult> HandleAsync(
        ProgressoOnboardingQuery request,
        RequestHandlerDelegate<ProgressoOnboardingQueryResult> next,
        CancellationToken cancellationToken) => next();
}
