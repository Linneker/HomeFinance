namespace Acme.Sistemas.Repository.Repositories.V1.Relatorio.Query;

internal static class RelatorioQuery
{
    /// <summary>
    /// Junta lançamento e participação e devolve o valor que cabe ao tenant.
    /// </summary>
    /// <remarks>
    /// <c>GROUP BY l.id</c> basta: <c>id</c> é chave primária, então o Postgres reconhece a
    /// dependência funcional e aceita as demais colunas na projeção. Agrupar antes de somar é o
    /// que impede a linha de aparecer duplicada quando houver mais de uma participação — hoje
    /// há sempre uma, e é exatamente essa suposição que não se pode deixar entranhada na
    /// consulta.
    /// <para>
    /// O recorte por tenant não aparece aqui porque as duas tabelas têm RLS forçada. Repetir a
    /// condição no SQL daria a impressão de que ela é o que protege — e um <c>WHERE</c>
    /// esquecido passaria despercebido em revisão.
    /// </para>
    /// </remarks>
    private static string Apurado(bool porCaixa, bool somenteRealizado)
    {
        var coluna = porCaixa ? "data_caixa" : "data_competencia";
        var realizado = somenteRealizado ? "AND l.previsto = false" : string.Empty;

        return $"""
            SELECT l.id,
                   l.tenant_id,
                   l.descricao,
                   l.valor,
                   l.data_competencia,
                   l.data_caixa,
                   l.categoria_id,
                   l.grupo_parcelamento_id,
                   l.parcela_numero,
                   l.parcela_total,
                   l.recorrencia_id,
                   l.previsto,
                   l.criado_em,
                   round(l.valor * sum(p.percentual) / 100.0, 2) AS valor_apurado
            FROM lancamento l
            JOIN lancamento_participacao p ON p.lancamento_id = l.id
            WHERE l.{coluna} BETWEEN @de AND @ate
              {realizado}
            GROUP BY l.id
            """;
    }

    public static string Extrato(bool porCaixa)
    {
        var coluna = porCaixa ? "data_caixa" : "data_competencia";

        return $"""
            {Apurado(porCaixa, somenteRealizado: false)}
            ORDER BY {coluna}, descricao
            """;
    }

    /// <summary>
    /// Total por categoria. A contagem é de lançamentos, não de participações — por isso a
    /// apuração vem de uma CTE já agrupada por lançamento.
    /// </summary>
    public static string TotaisPorCategoria(bool porCaixa) => $"""
        WITH apurado AS (
            {Apurado(porCaixa, somenteRealizado: true)}
        )
        SELECT categoria_id,
               sum(valor_apurado) AS realizado,
               count(*)           AS quantidade
        FROM apurado
        GROUP BY categoria_id
        ORDER BY categoria_id
        """;
}
