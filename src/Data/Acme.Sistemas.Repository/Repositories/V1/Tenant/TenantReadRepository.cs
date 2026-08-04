using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Tenant.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Tenant;

public sealed class TenantReadRepository(TransactionManager transacoes) : ITenantReadRepository
{
    public async Task<IReadOnlyList<TenantDisponivel>> ObterDisponiveisAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(TenantQuery.Disponiveis)
            .Com("usuarioId", usuarioId);

        var disponiveis = new List<TenantDisponivel>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            disponiveis.Add(new TenantDisponivel(
                leitor.Identificador("tenant_id"),
                leitor.GetBoolean(leitor.GetOrdinal("eh_proprio")),
                leitor.GetBoolean(leitor.GetOrdinal("somente_leitura"))));
        }

        return disponiveis;
    }

    public async Task<IReadOnlyList<TenantCompartilhamentoEntity>> ObterCompartilhamentosConcedidosAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(TenantQuery.CompartilhamentosConcedidos)
            .Com("tenantId", tenantId);

        var compartilhamentos = new List<TenantCompartilhamentoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            compartilhamentos.Add(new TenantCompartilhamentoEntity
            {
                Id = leitor.Identificador("id"),
                TenantId = leitor.Identificador("tenant_id"),
                UsuarioDestinatarioId = leitor.Identificador("usuario_destinatario_id"),
                IncluiScr = leitor.GetBoolean(leitor.GetOrdinal("inclui_scr")),
                ConcedidoEm = leitor.DataHora("concedido_em"),
                RevogadoEm = leitor.DataHoraOpcional("revogado_em"),
                CriadoEm = leitor.DataHora("concedido_em"),
            });
        }

        return compartilhamentos;
    }
}
