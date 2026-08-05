namespace Acme.Sistemas.Repository.Repositories.V1.Orcamento.Query;

internal static class OrcamentoQuery
{
    private const string Colunas =
        "id, tenant_id, categoria_id, competencia, valor_planejado, criado_em";

    public static string ListarDoMes => $"""
        SELECT {Colunas}
        FROM orcamento_mensal
        WHERE competencia = @competencia
        ORDER BY categoria_id
        """;
}
