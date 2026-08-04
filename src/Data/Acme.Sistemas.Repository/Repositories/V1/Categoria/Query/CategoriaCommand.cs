namespace Acme.Sistemas.Repository.Repositories.V1.Categoria.Query;

internal static class CategoriaCommand
{
    public const string Inserir = """
        INSERT INTO categoria (id, tenant_id, nome, criado_em)
        VALUES (@id, @tenantId, @nome, @criadoEm)
        """;
}
