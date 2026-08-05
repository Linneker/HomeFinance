using Acme.Sistemas.Domain.Entities.Onboarding;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>Leitura do onboarding para a barra de progresso.</summary>
public interface IOnboardingReadRepository
{
    /// <summary>Catálogo completo, inclusive as etapas ainda não ativas.</summary>
    Task<IReadOnlyList<OnboardingRequisitoEntity>> CatalogoAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OnboardingEstadoEntity>> EstadoDoTenantAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Escrita do estado e as leituras que decidem bloqueio, todas pelo primário.
/// </summary>
/// <remarks>
/// O bloqueio é avaliado a cada escrita de dado financeiro. Consultar réplica atrasada
/// recusaria o primeiro lançamento de quem acabou de declarar a renda — exatamente o momento em
/// que a pessoa está esperando o produto destravar.
/// </remarks>
public interface IOnboardingWriteRepository
{
    Task<IReadOnlyList<OnboardingRequisitoEntity>> CatalogoAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OnboardingEstadoEntity>> EstadoDoTenantAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Marca o requisito como cumprido. Repetir não muda a data do primeiro cumprimento.
    /// </summary>
    Task MarcarCumpridoAsync(
        OnboardingEstadoEntity estado,
        CancellationToken cancellationToken);
}
