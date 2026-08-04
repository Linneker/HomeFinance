namespace Acme.Sistemas.Repository.Repositories.V1.Recorrencia.Query;

internal static class RecorrenciaQuery
{
    private const string Colunas =
        "id, tenant_id, descricao, valor, dia_do_mes, categoria_id, inicio, encerrada_em, criado_em";

    public static string ListarAtivas => $"""
        SELECT {Colunas}
        FROM recorrencia
        WHERE encerrada_em IS NULL
        ORDER BY descricao
        """;

    public static string Obter => $"""
        SELECT {Colunas}
        FROM recorrencia
        WHERE id = @id
        """;
}
