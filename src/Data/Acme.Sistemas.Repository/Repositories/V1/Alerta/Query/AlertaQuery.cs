namespace Acme.Sistemas.Repository.Repositories.V1.Alerta.Query;

internal static class AlertaQuery
{
    private const string Colunas =
        "id, tenant_id, tipo, chave_condicao, estado, mensagem, criado_em, encerrado_em, dispensado_em";

    /// <summary>Só os visíveis. Dispensado não volta para a tela.</summary>
    public static string ListarAbertos => $"""
        SELECT {Colunas}
        FROM alerta
        WHERE estado = 1
        ORDER BY criado_em DESC
        """;

    /// <summary>
    /// Abertos e dispensados do tipo: os dois significam que a condição ainda vale.
    /// </summary>
    public static string ListarPorCondicaoAtiva => $"""
        SELECT {Colunas}
        FROM alerta
        WHERE tipo = @tipo
          AND estado IN (1, 3)
        """;

    public static string Obter => $"""
        SELECT {Colunas}
        FROM alerta
        WHERE id = @id
        """;

    public const string TenantsParaRotina = "SELECT tenant_id, usuario_id FROM tenants_para_rotina()";
}
