namespace Acme.Sistemas.Repository.Repositories.V1.Onboarding.Query;

internal static class OnboardingQuery
{
    /// <summary>
    /// Catálogo completo, inclusive o que ainda não está ativo — a barra mostra o caminho todo.
    /// </summary>
    public const string Catalogo = """
        SELECT codigo, descricao, ordem, ativo, bloqueante
        FROM onboarding_requisito
        ORDER BY ordem
        """;

    public const string EstadoDoTenant = """
        SELECT id, tenant_id, requisito_codigo, cumprido_em
        FROM onboarding_estado
        """;
}
