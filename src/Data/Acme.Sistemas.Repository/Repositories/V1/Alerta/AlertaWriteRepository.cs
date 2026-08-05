using Acme.Sistemas.Domain.Entities.Alertas;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Alerta.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Alerta;

public sealed class AlertaWriteRepository(TransactionManager transacoes) : IAlertaWriteRepository
{
    public async Task<IReadOnlyList<AlertaEntity>> ListarPorCondicaoAtivaAsync(
        TipoAlerta tipo,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AlertaQuery.ListarPorCondicaoAtiva)
            .Com("tipo", (short)tipo);

        return await AlertaReadRepository.LerTodosAsync(comando, cancellationToken);
    }

    public async Task<AlertaEntity?> ObterAsync(Guid alertaId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AlertaQuery.Obter)
            .Com("id", alertaId);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken) ? AlertaReadRepository.Ler(leitor) : null;
    }

    public async Task GravarAsync(AlertaEntity alerta, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AlertaCommand.Gravar)
            .Com("id", alerta.Id)
            .Com("tenantId", alerta.TenantId)
            .Com("tipo", (short)alerta.Tipo)
            .Com("chaveCondicao", alerta.ChaveCondicao)
            .Com("estado", (short)alerta.Estado)
            .Com("mensagem", alerta.Mensagem)
            .Com("criadoEm", alerta.CriadoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<bool> AtualizarEstadoAsync(
        AlertaEntity alerta,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AlertaCommand.AtualizarEstado)
            .Com("id", alerta.Id)
            .Com("estado", (short)alerta.Estado)
            .Com("encerradoEm", alerta.EncerradoEm?.UtcDateTime)
            .Com("dispensadoEm", alerta.DispensadoEm?.UtcDateTime);

        var afetadas = await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);

        return afetadas > 0;
    }
}
