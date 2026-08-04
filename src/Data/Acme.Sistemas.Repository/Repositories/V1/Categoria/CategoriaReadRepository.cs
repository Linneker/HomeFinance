using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Categoria.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Categoria;

public sealed class CategoriaReadRepository(TransactionManager transacoes) : ICategoriaReadRepository
{
    public async Task<IReadOnlyList<CategoriaEntity>> ListarAsync(CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(CategoriaQuery.Listar);

        var categorias = new List<CategoriaEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            categorias.Add(new CategoriaEntity
            {
                Id = leitor.Identificador("id"),
                TenantId = leitor.Identificador("tenant_id"),
                Nome = leitor.TextoObrigatorio("nome"),
                CriadoEm = leitor.DataHora("criado_em"),
            });
        }

        return categorias;
    }
}
