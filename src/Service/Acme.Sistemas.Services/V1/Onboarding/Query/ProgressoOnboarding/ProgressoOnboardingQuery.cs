using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Onboarding.Query.ProgressoOnboarding;

public sealed record ProgressoOnboardingQuery(Guid TenantId)
    : IRequest<ProgressoOnboardingQueryResult>;
