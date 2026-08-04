using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Tenant.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Tenant;

/// <summary>
/// Escrita de compartilhamento e as leituras que a decidem — todas pelo primário.
/// </summary>
/// <remarks>
/// <see cref="ResolverAcessoAsync"/> é o caso mais crítico: seu resultado define o contexto de
/// RLS da requisição inteira. Resolvê-lo contra réplica atrasada deixaria valer um
/// compartilhamento revogado segundos antes — exatamente a janela que a revogação existe para
/// fechar.
/// </remarks>
public sealed class TenantWriteRepository(TransactionManager transacoes) : ITenantWriteRepository
{
    public async Task<TenantDisponivel?> ResolverAcessoAsync(
        Guid usuarioId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(TenantQuery.ResolverAcesso)
            .Com("usuarioId", usuarioId)
            .Com("tenantId", tenantId);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        if (!await leitor.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new TenantDisponivel(
            leitor.Identificador("tenant_id"),
            leitor.GetBoolean(leitor.GetOrdinal("eh_proprio")),
            leitor.GetBoolean(leitor.GetOrdinal("somente_leitura")));
    }

    public async Task<Guid?> ObterTenantDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(TenantQuery.TenantDoUsuario)
            .Com("usuarioId", usuarioId);

        return await comando.ExecuteScalarAsync(cancellationToken) as Guid?;
    }

    public async Task<Guid?> ObterUsuarioPorEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(TenantQuery.UsuarioPorEmail)
            .Com("email", email);

        return await comando.ExecuteScalarAsync(cancellationToken) as Guid?;
    }

    public async Task<bool> ExisteCompartilhamentoVigenteAsync(
        Guid tenantId,
        Guid usuarioDestinatarioId,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(TenantQuery.ExisteVigente)
            .Com("tenantId", tenantId)
            .Com("usuarioDestinatarioId", usuarioDestinatarioId);

        return await comando.ExecuteScalarAsync(cancellationToken) is true;
    }

    public async Task ConcederAsync(
        TenantCompartilhamentoEntity compartilhamento,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(TenantCommand.Conceder)
            .Com("id", compartilhamento.Id)
            .Com("tenantId", compartilhamento.TenantId)
            .Com("usuarioDestinatarioId", compartilhamento.UsuarioDestinatarioId)
            .Com("incluiScr", compartilhamento.IncluiScr)
            .Com("concedidoEm", compartilhamento.ConcedidoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<bool> RevogarAsync(
        Guid tenantId,
        Guid usuarioDestinatarioId,
        DateTimeOffset revogadoEm,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(TenantCommand.Revogar)
            .Com("tenantId", tenantId)
            .Com("usuarioDestinatarioId", usuarioDestinatarioId)
            .Com("revogadoEm", revogadoEm.UtcDateTime);

        var afetadas = await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);

        return afetadas > 0;
    }
}
