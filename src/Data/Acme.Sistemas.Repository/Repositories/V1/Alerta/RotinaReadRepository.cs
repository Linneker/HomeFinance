using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Alerta.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Alerta;

/// <summary>
/// Enumeração dos tenants para a rotina diária, pela função <c>SECURITY DEFINER</c>.
/// </summary>
/// <remarks>
/// Usa a conexão primária porque decide um ciclo de trabalho que grava: ler a lista de uma
/// réplica atrasada deixaria de reavaliar a conta criada há instantes.
/// </remarks>
public sealed class RotinaReadRepository(TransactionManager transacoes) : IRotinaReadRepository
{
    public async Task<IReadOnlyList<TenantParaRotina>> TenantsAtivosAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AlertaQuery.TenantsParaRotina);

        var tenants = new List<TenantParaRotina>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            tenants.Add(new TenantParaRotina(
                leitor.Identificador("tenant_id"), leitor.Identificador("usuario_id")));
        }

        return tenants;
    }

    public async Task<Guid?> TenantDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AlertaQuery.TenantDoUsuarioParaRotina)
            .Com("usuarioId", usuarioId);

        return await comando.ExecuteScalarAsync(cancellationToken) as Guid?;
    }
}
