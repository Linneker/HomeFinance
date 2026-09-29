using Acme.Sistemas.Domain.Entities.Administracao;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Administracao.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Administracao;

/// <inheritdoc cref="IAdministradorRepository"/>
public sealed class AdministradorWriteRepository(TransactionManager transacoes) : IAdministradorRepository
{
    public async Task<CredenciaisAdministrativas?> ObterCredenciaisAsync(
        string email,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AdministracaoSql.CredenciaisPorEmail)
            .Com("email", email);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken)
            ? new CredenciaisAdministrativas(
                leitor.Identificador("administrador_id"),
                leitor.TextoObrigatorio("senha_hash"))
            : null;
    }

    public async Task ProvisionarAsync(
        AdministradorEntity administrador,
        string senhaHash,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AdministracaoSql.Provisionar)
            .Com("id", administrador.Id)
            .Com("email", administrador.Email)
            .Com("nome", administrador.Nome)
            .Com("senhaHash", senhaHash)
            .Com("criadoEm", administrador.CriadoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }
}
