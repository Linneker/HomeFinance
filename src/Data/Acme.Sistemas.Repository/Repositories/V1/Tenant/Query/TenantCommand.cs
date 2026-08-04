namespace Acme.Sistemas.Repository.Repositories.V1.Tenant.Query;

internal static class TenantCommand
{
    public const string Conceder = """
        INSERT INTO tenant_compartilhamento
            (id, tenant_id, usuario_destinatario_id, inclui_scr, concedido_em)
        VALUES (@id, @tenantId, @usuarioDestinatarioId, @incluiScr, @concedidoEm)
        """;

    /// <summary>
    /// Marca a revogação em vez de apagar: o histórico de quem teve acesso e quando é o que
    /// permite responder a uma contestação depois.
    /// </summary>
    public const string Revogar = """
        UPDATE tenant_compartilhamento
        SET revogado_em = @revogadoEm
        WHERE tenant_id = @tenantId
          AND usuario_destinatario_id = @usuarioDestinatarioId
          AND revogado_em IS NULL
        """;
}
