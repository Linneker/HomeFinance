namespace Acme.Sistemas.Repository.Repositories.V1.RegraClassificacao.Query;

internal static class RegraClassificacaoCommand
{
    /// <summary>
    /// Substitui a categoria quando já existe regra para o mesmo padrão no tenant.
    /// </summary>
    /// <remarks>
    /// O índice único é <c>(tenant_id, padrao)</c>, mas o <c>ON CONFLICT</c> cita só as
    /// colunas: a policy de RLS já restringe a linha alcançada ao tenant ativo, e nomear a
    /// restrição aqui a duplicaria em dois lugares que podem divergir.
    /// </remarks>
    public const string Gravar = """
        INSERT INTO regra_classificacao (id, tenant_id, padrao, categoria_id, criada_em)
        VALUES (@id, @tenantId, @padrao, @categoriaId, @criadaEm)
        ON CONFLICT (tenant_id, padrao) DO UPDATE
        SET categoria_id = EXCLUDED.categoria_id,
            criada_em    = EXCLUDED.criada_em
        """;

    public const string Alterar = """
        UPDATE regra_classificacao
        SET padrao = @padrao, categoria_id = @categoriaId
        WHERE id = @id
        """;

    public const string Remover = """
        DELETE FROM regra_classificacao WHERE id = @id
        """;
}
