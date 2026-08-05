namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Onboarding;

public sealed record PassoDoOnboardingResponse(
    string Codigo,
    string Descricao,
    int Ordem,
    bool Cumprido,
    bool Bloqueante,
    bool Futuro);

/// <param name="Liberado">
/// Não há bloqueio pendente. O app usa isto para habilitar as ações de escrita sem precisar
/// tentar e receber recusa.
/// </param>
public sealed record ProgressoResponse(
    IReadOnlyList<PassoDoOnboardingResponse> Passos,
    IReadOnlyList<PassoDoOnboardingResponse> Pendentes,
    int PercentualConcluido,
    bool Concluido,
    bool Liberado);
