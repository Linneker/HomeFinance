using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Categoria.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Categoria;

/// <summary>
/// Escrita de categoria — e as leituras que decidem essa escrita, pelo primário.
/// </summary>
public sealed class CategoriaWriteRepository(TransactionManager transacoes) : ICategoriaWriteRepository
{
    public async Task<bool> ExisteNomeAsync(string nome, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(CategoriaQuery.ExisteNome)
            .Com("nome", nome);

        return await comando.ExecuteScalarAsync(cancellationToken) is true;
    }

    public async Task<bool> ExisteAsync(Guid categoriaId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(CategoriaQuery.Existe)
            .Com("id", categoriaId);

        return await comando.ExecuteScalarAsync(cancellationToken) is true;
    }

    public async Task CriarAsync(CategoriaEntity categoria, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(CategoriaCommand.Inserir)
            .Com("id", categoria.Id)
            .Com("tenantId", categoria.TenantId)
            .Com("nome", categoria.Nome)
            .Com("criadoEm", categoria.CriadoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }
}
