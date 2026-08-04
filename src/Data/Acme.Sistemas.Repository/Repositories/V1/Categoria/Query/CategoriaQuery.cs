namespace Acme.Sistemas.Repository.Repositories.V1.Categoria.Query;

internal static class CategoriaQuery
{
    /// <summary>
    /// Sem <c>WHERE tenant_id</c>: a policy de RLS acrescenta o predicado. Repetir aqui
    /// sugeriria que o isolamento depende deste SELECT — e é justamente o contrário.
    /// </summary>
    public const string Listar = """
        SELECT id, tenant_id, nome, criado_em
        FROM categoria
        ORDER BY nome
        """;

    public const string ExisteNome = """
        SELECT EXISTS (SELECT 1 FROM categoria WHERE lower(nome) = lower(@nome))
        """;

    public const string Existe = """
        SELECT EXISTS (SELECT 1 FROM categoria WHERE id = @id)
        """;
}
