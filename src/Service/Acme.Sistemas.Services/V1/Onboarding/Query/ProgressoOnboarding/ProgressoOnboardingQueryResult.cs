namespace Acme.Sistemas.Services.V1.Onboarding.Query.ProgressoOnboarding;

/// <param name="Futuro">
/// Etapa anunciada e ainda não ativa: aparece na barra, não conta no percentual, não bloqueia.
/// </param>
public sealed record PassoListado(
    string Codigo,
    string Descricao,
    int Ordem,
    bool Cumprido,
    bool Bloqueante,
    bool Futuro);

/// <param name="Liberado">
/// Não há bloqueio pendente. É diferente de <paramref name="Concluido"/>: dá para estar
/// liberado com etapas ativas ainda por cumprir, desde que nenhuma delas seja bloqueante.
/// </param>
public sealed record ProgressoOnboardingQueryResult(
    IReadOnlyList<PassoListado> Passos,
    IReadOnlyList<PassoListado> Pendentes,
    int PercentualConcluido,
    bool Concluido,
    bool Liberado)
{
    public static readonly ProgressoOnboardingQueryResult Vazio =
        new([], [], 0, false, false);
}
