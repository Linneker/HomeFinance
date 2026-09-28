using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Exportacao.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Exportacao;

/// <inheritdoc cref="IExportacaoSolicitacaoWriteRepository"/>
public sealed class ExportacaoSolicitacaoWriteRepository(TransactionManager transacoes)
    : IExportacaoSolicitacaoWriteRepository
{
    public async Task CriarAsync(
        ExportacaoSolicitacaoEntity solicitacao,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoSolicitacaoCommand.Criar)
            .Com("id", solicitacao.Id)
            .Com("tenantId", solicitacao.TenantId)
            .Com("usuarioSolicitanteId", solicitacao.UsuarioSolicitanteId)
            .Com("formato", (short)solicitacao.Formato)
            .Com("situacao", (short)solicitacao.Situacao)
            .Com("criadoEm", solicitacao.CriadoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<ExportacaoSolicitacaoEntity?> ObterAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoSolicitacaoQuery.PorId)
            .Com("id", id);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken)
            ? ExportacaoSolicitacaoMap.Ler(leitor)
            : null;
    }

    public async Task AtualizarAsync(
        ExportacaoSolicitacaoEntity solicitacao,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoSolicitacaoCommand.Atualizar)
            .Com("id", solicitacao.Id)
            .Com("situacao", (short)solicitacao.Situacao)
            .Com("referenciaArquivo", solicitacao.ReferenciaArquivo)
            .Com("nomeArquivo", solicitacao.NomeArquivo)
            .Com("concluidoEm", solicitacao.ConcluidoEm?.UtcDateTime)
            .Com("expiraEm", solicitacao.ExpiraEm?.UtcDateTime)
            .Com("erro", solicitacao.Erro);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SolicitacaoExpirada>> ExpiradasAsync(
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoSolicitacaoQuery.Expiradas)
            .Com("agora", agora.UtcDateTime);

        var lista = new List<SolicitacaoExpirada>();
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            lista.Add(new SolicitacaoExpirada(
                leitor.Identificador("id"),
                leitor.Identificador("tenant_id"),
                leitor.IsDBNull(leitor.GetOrdinal("referencia_arquivo"))
                    ? null
                    : leitor.TextoObrigatorio("referencia_arquivo")));
        }

        return lista;
    }

    public async Task ExpirarAsync(Guid solicitacaoId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoSolicitacaoCommand.Expirar)
            .Com("id", solicitacaoId);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<int> CancelarPendentesAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoSolicitacaoCommand.CancelarPendentes)
            .Com("usuarioId", usuarioId);

        var canceladas = Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken));
        await escopo.ConfirmarAsync(cancellationToken);

        return canceladas;
    }
}
