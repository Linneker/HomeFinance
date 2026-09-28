namespace Acme.Sistemas.Repository.Repositories.V1.Exportacao.Query;

internal static class ExportacaoHabilitacaoSql
{
    /// <summary>
    /// Habilitada de verdade: a linha existe e está ligada. Sob a policy, só a própria conta
    /// enxerga a própria habilitação, então o filtro por usuário é a RLS, não o WHERE.
    /// </summary>
    public const string Habilitada = """
        SELECT EXISTS (
            SELECT 1 FROM exportacao_habilitacao
            WHERE usuario_id = @usuarioId AND habilitada = true
        )
        """;

    /// <summary>
    /// Upsert pela função SECURITY DEFINER. O app não tem INSERT/UPDATE direto na tabela — este é
    /// o único caminho de escrita, e ele parte só do endpoint autenticado como administrador.
    /// </summary>
    public const string Definir = """
        SELECT definir_habilitacao_exportacao(
            @id, @usuarioId, @habilitada, @motivo, @autorId, @quando)
        """;
}
