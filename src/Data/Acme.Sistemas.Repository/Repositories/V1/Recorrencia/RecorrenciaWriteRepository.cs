using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento.Query;
using Acme.Sistemas.Repository.Repositories.V1.Recorrencia.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Recorrencia;

/// <summary>
/// Escrita da regra e materialização das ocorrências previstas.
/// </summary>
/// <remarks>
/// Regra e ocorrências vão no mesmo commit: uma regra sem ocorrências não projeta saldo e não
/// dispara alerta de caixa, que é a razão de materializar em vez de calcular sob demanda.
/// </remarks>
public sealed class RecorrenciaWriteRepository(TransactionManager transacoes) : IRecorrenciaWriteRepository
{
    public async Task<RecorrenciaEntity?> ObterAsync(Guid recorrenciaId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(RecorrenciaQuery.Obter)
            .Com("id", recorrenciaId);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken) ? RecorrenciaReadRepository.Ler(leitor) : null;
    }

    public async Task CriarAsync(
        RecorrenciaEntity recorrencia,
        IReadOnlyList<LancamentoEntity> ocorrencias,
        IReadOnlyList<LancamentoParticipacaoEntity> participacoes,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        var sql = new EscopoSql(escopo.Conexao, escopo.Transacao);

        await using (var comando = sql.Comando(RecorrenciaCommand.Inserir)
                         .Com("id", recorrencia.Id)
                         .Com("tenantId", recorrencia.TenantId)
                         .Com("descricao", recorrencia.Descricao)
                         .Com("valor", recorrencia.Valor)
                         .Com("diaDoMes", (short)recorrencia.DiaDoMes)
                         .Com("categoriaId", recorrencia.CategoriaId)
                         .Com("inicio", recorrencia.Inicio)
                         .Com("criadoEm", recorrencia.CriadoEm.UtcDateTime))
        {
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var ocorrencia in ocorrencias)
        {
            await using var comando = sql.Comando(LancamentoCommand.Inserir).ComLancamento(ocorrencia);
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

        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<bool> EncerrarAsync(
        Guid recorrenciaId,
        DateOnly aPartirDe,
        DateTimeOffset encerradaEm,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        var sql = new EscopoSql(escopo.Conexao, escopo.Transacao);

        int encerradas;

        await using (var comando = sql.Comando(RecorrenciaCommand.Encerrar)
                         .Com("id", recorrenciaId)
                         .Com("encerradaEm", encerradaEm.UtcDateTime))
        {
            encerradas = await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        if (encerradas == 0)
        {
            // Já estava encerrada. Não apaga nada: as previstas que sobraram, se sobraram,
            // foram deixadas por um corte anterior e apagá-las agora mudaria o passado.
            return false;
        }

        await using (var comando = sql.Comando(RecorrenciaCommand.RemoverPrevistasAPartirDe)
                         .Com("recorrenciaId", recorrenciaId)
                         .Com("aPartirDe", aPartirDe))
        {
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        await escopo.ConfirmarAsync(cancellationToken);

        return true;
    }
}
