namespace Acme.Sistemas.Domain.Entities.Onboarding;

/// <param name="Futuro">
/// Etapa anunciada e ainda não ativa. Aparece na barra, não conta no percentual e não bloqueia.
/// </param>
public sealed record PassoDoOnboarding(
    string Codigo,
    string Descricao,
    int Ordem,
    bool Cumprido,
    bool Bloqueante,
    bool Futuro);

/// <summary>
/// Situação do onboarding de um tenant.
/// </summary>
public sealed record ProgressoDoOnboarding(IReadOnlyList<PassoDoOnboarding> Passos)
{
    private IEnumerable<PassoDoOnboarding> Ativos => Passos.Where(passo => !passo.Futuro);

    /// <summary>
    /// Percentual sobre os passos ativos.
    /// </summary>
    /// <remarks>
    /// As etapas futuras ficam fora da conta. Incluí-las travaria a barra num teto que a pessoa
    /// não tem como alcançar — ela faria tudo o que o produto pede e ainda veria "60%".
    /// </remarks>
    public int PercentualConcluido
    {
        get
        {
            var total = Ativos.Count();

            return total == 0
                ? 100
                : (int)Math.Round(100m * Ativos.Count(passo => passo.Cumprido) / total);
        }
    }

    public IReadOnlyList<PassoDoOnboarding> Pendentes =>
        [.. Ativos.Where(passo => !passo.Cumprido).OrderBy(passo => passo.Ordem)];

    /// <summary>Requisitos que impedem a escrita de dado financeiro agora.</summary>
    public IReadOnlyList<PassoDoOnboarding> Bloqueios =>
        [.. Pendentes.Where(passo => passo.Bloqueante)];

    public bool Concluido => Pendentes.Count == 0;

    public bool Liberado => Bloqueios.Count == 0;

    /// <summary>
    /// Monta o progresso cruzando o catálogo com o que o tenant já cumpriu.
    /// </summary>
    /// <remarks>
    /// A ausência de estado significa não cumprido — é o que faz um requisito novo valer para
    /// as contas antigas sem migração de dados nenhuma.
    /// </remarks>
    public static ProgressoDoOnboarding Montar(
        IEnumerable<OnboardingRequisitoEntity> catalogo,
        IEnumerable<OnboardingEstadoEntity> estados)
    {
        var cumpridos = estados
            .Where(estado => estado.Cumprido)
            .Select(estado => estado.RequisitoCodigo)
            .ToHashSet(StringComparer.Ordinal);

        return new ProgressoDoOnboarding(
            [.. catalogo
                .OrderBy(requisito => requisito.Ordem)
                .Select(requisito => new PassoDoOnboarding(
                    requisito.Codigo,
                    requisito.Descricao,
                    requisito.Ordem,
                    cumpridos.Contains(requisito.Codigo),
                    requisito.BloqueiaAgora,
                    !requisito.Ativo))]);
    }
}
