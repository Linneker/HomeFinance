namespace Acme.Sistemas.Repository.Repositories.V1.Lancamento.Query;

internal static class LancamentoQuery
{
    private const string Colunas = """
        id, tenant_id, descricao, valor, data_competencia, data_caixa, categoria_id,
        origem_classificacao, grupo_parcelamento_id, parcela_numero, parcela_total,
        recorrencia_id, previsto, criado_em, alterado_em
        """;

    /// <summary>
    /// Extrato do período. A escolha entre competência e caixa é do chamador e vira a coluna
    /// filtrada e ordenada — as duas visões são a mesma consulta sobre colunas diferentes,
    /// sem ramo condicional por tipo de lançamento.
    /// </summary>
    public static string Listar(bool porCaixa, bool filtrarCategoria)
    {
        var coluna = porCaixa ? "data_caixa" : "data_competencia";
        var categoria = filtrarCategoria ? "AND categoria_id = @categoriaId" : string.Empty;

        return $"""
            SELECT {Colunas}
            FROM lancamento
            WHERE {coluna} BETWEEN @de AND @ate
              {categoria}
            ORDER BY {coluna}, descricao
            """;
    }

    public static string Obter => $"""
        SELECT {Colunas}
        FROM lancamento
        WHERE id = @id
        """;

    /// <summary>
    /// Possíveis duplicados. Descrição comparada sem diferenciar caixa nem espaço nas pontas:
    /// "Mercado " e "mercado" são a mesma digitação para quem está conferindo a fatura.
    /// </summary>
    public const string ContarSemelhantes = """
        SELECT count(*)
        FROM lancamento
        WHERE lower(btrim(descricao)) = lower(btrim(@descricao))
          AND valor = @valor
          AND data_competencia = @dataCompetencia
        """;

    public const string ListarParticipacoes = """
        SELECT id, tenant_id, lancamento_id, usuario_id, percentual
        FROM lancamento_participacao
        WHERE lancamento_id = @lancamentoId
        """;
}
