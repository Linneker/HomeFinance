using Acme.Sistemas.Services.V1.Onboarding.Query.ProgressoOnboarding;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Onboarding;

internal static class OnboardingMap
{
    public static ProgressoResponse ParaResponse(this ProgressoOnboardingQueryResult resultado) =>
        new(
            [.. resultado.Passos.Select(Listar)],
            [.. resultado.Pendentes.Select(Listar)],
            resultado.PercentualConcluido,
            resultado.Concluido,
            resultado.Liberado);

    private static PassoDoOnboardingResponse Listar(PassoListado passo) =>
        new(passo.Codigo, passo.Descricao, passo.Ordem, passo.Cumprido, passo.Bloqueante, passo.Futuro);
}
