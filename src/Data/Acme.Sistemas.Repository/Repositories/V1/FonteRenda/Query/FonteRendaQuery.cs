namespace Acme.Sistemas.Repository.Repositories.V1.FonteRenda.Query;

internal static class FonteRendaQuery
{
    private const string Colunas =
        "id, tenant_id, nome, valor, dia_recebimento, vigencia_inicio, vigencia_fim, criado_em";

    /// <summary>
    /// Fontes do tenant, opcionalmente só as vigentes num mês.
    /// </summary>
    /// <remarks>
    /// O filtro é montado aqui, e não com <c>@competencia IS NULL OR ...</c>, porque a segunda
    /// forma impede o planejador de usar o índice de vigência: ele não sabe, no momento do
    /// plano, qual dos dois ramos vai valer.
    /// </remarks>
    public static string Listar(bool porCompetencia) => porCompetencia
        ? $"""
            SELECT {Colunas}
            FROM fonte_renda
            WHERE vigencia_inicio <= @competencia
              AND (vigencia_fim IS NULL OR vigencia_fim >= @competencia)
            ORDER BY nome, vigencia_inicio
            """
        : $"""
            SELECT {Colunas}
            FROM fonte_renda
            ORDER BY nome, vigencia_inicio
            """;

    public static string Obter => $"""
        SELECT {Colunas}
        FROM fonte_renda
        WHERE id = @id
        """;
}
