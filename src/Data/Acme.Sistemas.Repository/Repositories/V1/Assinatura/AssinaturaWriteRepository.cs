using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Assinatura.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Assinatura;

public sealed class AssinaturaWriteRepository(TransactionManager transacoes)
    : IAssinaturaWriteRepository
{
    public async Task<AssinaturaEntity?> VigenteDoUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaQuery.VigenteDoUsuario)
            .Com("usuarioId", usuarioId);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken)
            ? AssinaturaReadRepository.Ler(leitor)
            : null;
    }

    public async Task<AssinaturaEntity?> PorReferenciaAsync(
        CanalPagamento canal,
        string referencia,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaQuery.PorReferencia)
            .Com("canal", (short)canal)
            .Com("referencia", referencia);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken)
            ? AssinaturaReadRepository.Ler(leitor)
            : null;
    }

    public async Task CriarAsync(AssinaturaEntity assinatura, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaCommand.Criar)
            .Com("id", assinatura.Id)
            .Com("usuarioId", assinatura.UsuarioId)
            .Com("planoId", assinatura.PlanoId)
            .Com("situacao", (short)assinatura.Situacao)
            .Com("canal", (short)assinatura.Canal)
            .Com("periodoFim", assinatura.PeriodoFim.UtcDateTime)
            .Com("carenciaAte", assinatura.CarenciaAte?.UtcDateTime)
            .Com("comprovanteReferencia", assinatura.ComprovanteReferencia)
            .Com("criadoEm", assinatura.CriadoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<bool> AtualizarSituacaoAsync(
        AssinaturaEntity assinatura,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaCommand.AplicarSituacao)
            .Com("id", assinatura.Id)
            .Com("situacao", (short)assinatura.Situacao)
            .Com("periodoFim", assinatura.PeriodoFim.UtcDateTime)
            .Com("carenciaAte", assinatura.CarenciaAte?.UtcDateTime)
            .Com("atualizadoEm", (assinatura.AtualizadoEm ?? assinatura.CriadoEm).UtcDateTime);

        var afetadas = Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken));
        await escopo.ConfirmarAsync(cancellationToken);

        return afetadas > 0;
    }

    public async Task<int> ContarCompartilhamentosVigentesAsync(CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaQuery.ContarCompartilhamentosVigentes);

        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken));
    }

    public async Task RegistrarTransacaoAsync(
        TransacaoPagamentoEntity transacao,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaCommand.RegistrarTransacao)
            .Com("id", transacao.Id)
            .Com("assinaturaId", transacao.AssinaturaId)
            .Com("usuarioId", transacao.UsuarioId)
            .Com("canal", (short)transacao.Canal)
            .Com("identificadorProvedor", transacao.IdentificadorProvedor)
            .Com("valor", transacao.Valor)
            .Com("situacao", transacao.Situacao)
            .Com("criadoEm", transacao.CriadoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<bool> RegistrarNotificacaoAsync(
        CanalPagamento canal,
        string identificador,
        string corpo,
        DateTimeOffset recebidaEm,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaCommand.RegistrarNotificacao)
            .Com("id", Guid.CreateVersion7())
            .Com("canal", (short)canal)
            .Com("identificador", identificador)
            .Com("corpo", corpo)
            .Com("recebidaEm", recebidaEm.UtcDateTime);

        var afetadas = await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);

        // Zero linhas significa que esta notificação já tinha chegado antes.
        return afetadas > 0;
    }

    public async Task<AssinaturaEntity?> DoDonoDoTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaQuery.DoDonoDoTenant)
            .Com("tenantId", tenantId);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        if (!await leitor.ReadAsync(cancellationToken))
        {
            return null;
        }

        // A função devolve o mínimo: o resto do objeto fica com valores neutros de propósito,
        // porque expor plano e canal do dono a quem só recebeu leitura seria vazamento.
        return new AssinaturaEntity
        {
            Situacao = (SituacaoAssinatura)leitor.GetInt16(leitor.GetOrdinal("situacao")),
            PeriodoFim = leitor.DataHora("periodo_fim"),
            CarenciaAte = leitor.DataHoraOpcional("carencia_ate"),
            ComprovanteReferencia = string.Empty,
        };
    }

    public async Task<IReadOnlyList<AssinaturaEntity>> VigentesParaReconciliacaoAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaQuery.ParaReconciliacao);

        var assinaturas = new List<AssinaturaEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            assinaturas.Add(AssinaturaReadRepository.Ler(leitor));
        }

        return assinaturas;
    }
}
