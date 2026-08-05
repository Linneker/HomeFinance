namespace Acme.Sistemas.Repository.Repositories.V1.Onboarding.Query;

internal static class OnboardingCommand
{
    /// <summary>
    /// Marca o requisito como cumprido, uma vez.
    /// </summary>
    /// <remarks>
    /// <c>DO NOTHING</c> e não <c>DO UPDATE</c>: cadastrar a segunda fonte de renda não pode
    /// mover a data do primeiro cumprimento. Ela é o registro de quando a conta destravou.
    /// </remarks>
    public const string MarcarCumprido = """
        INSERT INTO onboarding_estado (id, tenant_id, requisito_codigo, cumprido_em)
        VALUES (@id, @tenantId, @requisitoCodigo, @cumpridoEm)
        ON CONFLICT (tenant_id, requisito_codigo) DO NOTHING
        """;
}
