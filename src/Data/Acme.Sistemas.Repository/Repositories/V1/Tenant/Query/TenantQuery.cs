namespace Acme.Sistemas.Repository.Repositories.V1.Tenant.Query;

internal static class TenantQuery
{
    /// <summary>
    /// Tenants selecionáveis pelo usuário. O próprio vem primeiro; os recebidos por
    /// compartilhamento entram como somente leitura.
    /// </summary>
    public const string Disponiveis = """
        SELECT t.id AS tenant_id, true AS eh_proprio, false AS somente_leitura
        FROM tenant t
        WHERE t.usuario_id = @usuarioId

        UNION ALL

        SELECT c.tenant_id, false, true
        FROM tenant_compartilhamento c
        WHERE c.usuario_destinatario_id = @usuarioId
          AND c.revogado_em IS NULL
        """;

    /// <summary>
    /// Resolve o acesso a um tenant específico. Devolve no máximo uma linha, com o modo mais
    /// permissivo primeiro: ser dono vence compartilhamento recebido.
    /// </summary>
    public const string ResolverAcesso = """
        SELECT tenant_id, eh_proprio, somente_leitura FROM (
            SELECT t.id AS tenant_id, true AS eh_proprio, false AS somente_leitura, 0 AS ordem
            FROM tenant t
            WHERE t.usuario_id = @usuarioId AND t.id = @tenantId

            UNION ALL

            SELECT c.tenant_id, false, true, 1
            FROM tenant_compartilhamento c
            WHERE c.usuario_destinatario_id = @usuarioId
              AND c.tenant_id = @tenantId
              AND c.revogado_em IS NULL
        ) acesso
        ORDER BY ordem
        LIMIT 1
        """;

    public const string TenantDoUsuario = """
        SELECT id FROM tenant WHERE usuario_id = @usuarioId
        """;

    /// <summary>
    /// Destinatário pelo e-mail. Usa a função SECURITY DEFINER porque a policy de `usuario`
    /// só deixa a pessoa ver a si mesma — e conceder acesso exige localizar outra.
    /// </summary>
    public const string UsuarioPorEmail = """
        SELECT usuario_id FROM credenciais_por_email(@email)
        """;

    public const string ExisteVigente = """
        SELECT EXISTS (
            SELECT 1 FROM tenant_compartilhamento
            WHERE tenant_id = @tenantId
              AND usuario_destinatario_id = @usuarioDestinatarioId
              AND revogado_em IS NULL
        )
        """;

    public const string CompartilhamentosConcedidos = """
        SELECT id, tenant_id, usuario_destinatario_id, inclui_scr, concedido_em, revogado_em
        FROM tenant_compartilhamento
        WHERE tenant_id = @tenantId
        ORDER BY concedido_em DESC
        """;
}
