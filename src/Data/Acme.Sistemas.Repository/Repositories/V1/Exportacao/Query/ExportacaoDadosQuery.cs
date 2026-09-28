namespace Acme.Sistemas.Repository.Repositories.V1.Exportacao.Query;

/// <summary>
/// SELECTs do conjunto exportável. Sem <c>WHERE tenant_id</c>: a RLS já restringe ao tenant do
/// contexto, e repetir o filtro na mão criaria uma segunda linha de defesa que diverge da
/// primeira. O tipo do lançamento é derivado das colunas de ligação, como no resto do sistema.
/// </summary>
internal static class ExportacaoDadosQuery
{
    /// <summary>
    /// Só os realizados. As ocorrências ainda previstas de uma recorrência são projeção, não
    /// registro — levá-las na exportação entregaria como fato o que ainda não aconteceu.
    /// </summary>
    public const string Lancamentos = """
        SELECT l.data_competencia,
               l.data_caixa,
               l.descricao,
               l.valor,
               CASE
                   WHEN l.grupo_parcelamento_id IS NOT NULL THEN 'Parcela'
                   WHEN l.recorrencia_id IS NOT NULL THEN 'Recorrente'
                   ELSE 'Avulso'
               END AS tipo,
               c.nome AS categoria
        FROM lancamento l
        LEFT JOIN categoria c ON c.id = l.categoria_id
        WHERE l.previsto = false
        ORDER BY l.data_competencia, l.criado_em
        """;

    public const string Categorias = """
        SELECT nome
        FROM categoria
        ORDER BY nome
        """;

    public const string Orcamentos = """
        SELECT o.competencia, c.nome AS categoria, o.valor_planejado
        FROM orcamento_mensal o
        JOIN categoria c ON c.id = o.categoria_id
        ORDER BY o.competencia, c.nome
        """;

    public const string FontesRenda = """
        SELECT nome, valor, vigencia_inicio, vigencia_fim
        FROM fonte_renda
        ORDER BY vigencia_inicio, nome
        """;
}
