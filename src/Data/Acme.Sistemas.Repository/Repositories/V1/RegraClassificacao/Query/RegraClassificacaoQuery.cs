namespace Acme.Sistemas.Repository.Repositories.V1.RegraClassificacao.Query;

internal static class RegraClassificacaoQuery
{
    private const string Colunas = "id, tenant_id, padrao, categoria_id, criada_em";

    /// <summary>
    /// Já vem na ordem de precedência: padrão mais longo primeiro, empate pela mais recente.
    /// Ordenar aqui evita reordenar em memória a cada lançamento gravado.
    /// </summary>
    public static string Listar => $"""
        SELECT {Colunas}
        FROM regra_classificacao
        ORDER BY length(padrao) DESC, criada_em DESC
        """;

    public static string Obter => $"""
        SELECT {Colunas}
        FROM regra_classificacao
        WHERE id = @id
        """;
}
