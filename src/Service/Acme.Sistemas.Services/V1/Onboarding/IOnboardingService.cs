using Acme.Sistemas.Domain.Entities.Onboarding;

namespace Acme.Sistemas.Services.V1.Onboarding;

/// <summary>
/// Estado do onboarding do tenant ativo e o cumprimento automático de requisitos.
/// </summary>
public interface IOnboardingService
{
    Task<ProgressoDoOnboarding> ProgressoAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Registra o cumprimento de um requisito, se ainda não estiver registrado.
    /// </summary>
    Task CumprirAsync(string requisitoCodigo, CancellationToken cancellationToken);
}
