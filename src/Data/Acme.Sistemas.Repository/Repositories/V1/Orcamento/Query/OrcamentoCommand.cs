namespace Acme.Sistemas.Repository.Repositories.V1.Orcamento.Query;

internal static class OrcamentoCommand
{
    /// <summary>
    /// Define o valor da categoria no mês. Redefinir substitui, apoiado no índice único
    /// <c>(tenant_id, categoria_id, competencia)</c>.
    /// </summary>
    public const string Gravar = """
        INSERT INTO orcamento_mensal
            (id, tenant_id, categoria_id, competencia, valor_planejado, criado_em)
        VALUES
            (@id, @tenantId, @categoriaId, @competencia, @valorPlanejado, @criadoEm)
        ON CONFLICT (tenant_id, categoria_id, competencia) DO UPDATE
        SET valor_planejado = EXCLUDED.valor_planejado
        """;

    public const string ApagarMes = """
        DELETE FROM orcamento_mensal
        WHERE competencia = @competencia
        """;

    public const string Remover = """
        DELETE FROM orcamento_mensal
        WHERE id = @id
        """;
}
