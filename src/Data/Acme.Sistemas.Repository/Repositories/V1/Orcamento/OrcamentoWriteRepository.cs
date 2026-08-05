using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Orcamento.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Orcamento;

public sealed class OrcamentoWriteRepository(TransactionManager transacoes) : IOrcamentoWriteRepository
{
    public async Task<IReadOnlyList<OrcamentoMensalEntity>> ListarDoMesAsync(
        DateOnly competencia,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(OrcamentoQuery.ListarDoMes)
            .Com("competencia", competencia);

        return await OrcamentoReadRepository.LerTodosAsync(comando, cancellationToken);
    }

    public async Task GravarAsync(
        OrcamentoMensalEntity orcamento,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = Inserir(new EscopoSql(escopo.Conexao, escopo.Transacao), orcamento);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<int> SubstituirMesAsync(
        DateOnly competencia,
        IReadOnlyList<OrcamentoMensalEntity> orcamentos,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        var sql = new EscopoSql(escopo.Conexao, escopo.Transacao);

        await using (var limpeza = sql
            .Comando(OrcamentoCommand.ApagarMes)
            .Com("competencia", competencia))
        {
            await limpeza.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var orcamento in orcamentos)
        {
            await using var insercao = Inserir(sql, orcamento);
            await insercao.ExecuteNonQueryAsync(cancellationToken);
        }

        await escopo.ConfirmarAsync(cancellationToken);

        return orcamentos.Count;
    }

    public async Task<bool> RemoverAsync(Guid orcamentoId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(OrcamentoCommand.Remover)
            .Com("id", orcamentoId);

        var afetadas = await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);

        return afetadas > 0;
    }

    private static DbCommand Inserir(EscopoSql sql, OrcamentoMensalEntity orcamento) =>
        sql.Comando(OrcamentoCommand.Gravar)
            .Com("id", orcamento.Id)
            .Com("tenantId", orcamento.TenantId)
            .Com("categoriaId", orcamento.CategoriaId)
            .Com("competencia", orcamento.Competencia)
            .Com("valorPlanejado", orcamento.ValorPlanejado)
            .Com("criadoEm", orcamento.CriadoEm.UtcDateTime);
}
