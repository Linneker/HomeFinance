using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Onboarding;

namespace Acme.Sistemas.Services.V1.Onboarding.Query.ProgressoOnboarding;

/// <summary>
/// Barra de progresso do onboarding do tenant ativo.
/// </summary>
/// <remarks>
/// Devolve o caminho inteiro, e não só o que falta: ver as etapas futuras é o que transforma
/// uma lista de exigências numa trilha com fim visível.
/// </remarks>
public sealed class ProgressoOnboardingQueryHandler(IOnboardingService onboarding)
    : IRequestHandler<ProgressoOnboardingQuery, ProgressoOnboardingQueryResult>
{
    public async Task<ProgressoOnboardingQueryResult> HandleAsync(
        ProgressoOnboardingQuery request,
        CancellationToken cancellationToken)
    {
        if (ProgressoOnboardingQueryValidation.Validar(request).Count > 0)
        {
            return ProgressoOnboardingQueryResult.Vazio;
        }

        var progresso = await onboarding.ProgressoAsync(cancellationToken);

        return new ProgressoOnboardingQueryResult(
            [.. progresso.Passos.Select(Listar)],
            [.. progresso.Pendentes.Select(Listar)],
            progresso.PercentualConcluido,
            progresso.Concluido,
            progresso.Liberado);
    }

    private static PassoListado Listar(PassoDoOnboarding passo) =>
        new(passo.Codigo, passo.Descricao, passo.Ordem, passo.Cumprido, passo.Bloqueante, passo.Futuro);
}
