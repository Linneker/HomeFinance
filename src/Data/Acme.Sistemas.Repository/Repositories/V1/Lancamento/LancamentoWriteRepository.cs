using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Lancamento;

/// <summary>
/// Escrita de lançamento — e as leituras que decidem essa escrita, pelo primário.
/// </summary>
/// <remarks>
/// <see cref="ContarSemelhantesAsync"/> e <see cref="ObterAsync"/> ficam aqui, e não no
/// repositório de leitura, porque decidem a escrita: contra réplica atrasada a verificação de
/// duplicidade não enxergaria o lançamento gravado segundos antes, e o aviso que existe
/// justamente para evitar a segunda digitação nunca apareceria.
/// </remarks>
public sealed class LancamentoWriteRepository(TransactionManager transacoes) : ILancamentoWriteRepository
{
    public async Task CriarAsync(
        IReadOnlyList<LancamentoEntity> lancamentos,
        IReadOnlyList<LancamentoParticipacaoEntity> participacoes,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        var sql = new EscopoSql(escopo.Conexao, escopo.Transacao);

        foreach (var lancamento in lancamentos)
        {
            await using var comando = sql.Comando(LancamentoCommand.Inserir).ComLancamento(lancamento);
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var participacao in participacoes)
        {
            await using var comando = sql.Comando(LancamentoCommand.InserirParticipacao)
                .Com("id", participacao.Id)
                .Com("tenantId", participacao.TenantId)
                .Com("lancamentoId", participacao.LancamentoId)
                .Com("usuarioId", participacao.UsuarioId)
                .Com("percentual", participacao.Percentual);

            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        // Um commit só: as parcelas de uma compra são a mesma compra, e lançamento sem
        // participação não é apurável por relatório nenhum.
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<LancamentoEntity?> ObterAsync(Guid lancamentoId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(LancamentoQuery.Obter)
            .Com("id", lancamentoId);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken) ? LancamentoMaterializador.Ler(leitor) : null;
    }

    public async Task<int> ContarSemelhantesAsync(
        string descricao,
        decimal valor,
        DateOnly dataCompetencia,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(LancamentoQuery.ContarSemelhantes)
            .Com("descricao", descricao)
            .Com("valor", valor)
            .Com("dataCompetencia", dataCompetencia);

        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> AlterarAsync(LancamentoEntity lancamento, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(LancamentoCommand.Alterar)
            .Com("id", lancamento.Id)
            .Com("descricao", lancamento.Descricao)
            .Com("valor", lancamento.Valor)
            .Com("dataCompetencia", lancamento.DataCompetencia)
            .Com("dataCaixa", lancamento.DataCaixa)
            .Com("categoriaId", lancamento.CategoriaId)
            .Com("origemClassificacao", (short)lancamento.OrigemClassificacao)
            .Com("alteradoEm", lancamento.AlteradoEm?.UtcDateTime);

        var afetadas = await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);

        return afetadas > 0;
    }

    public async Task<bool> RemoverAsync(Guid lancamentoId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(LancamentoCommand.Remover)
            .Com("id", lancamentoId);

        var afetadas = await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);

        return afetadas > 0;
    }
}
