using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Lancamento;

/// <summary>
/// Leitura das participações. É daqui que sai a apuração de valor por pessoa nos relatórios,
/// e não do <c>tenant_id</c> do lançamento.
/// </summary>
public sealed class LancamentoParticipacaoReadRepository(TransactionManager transacoes)
    : ILancamentoParticipacaoReadRepository
{
    public async Task<IReadOnlyList<LancamentoParticipacaoEntity>> ListarPorLancamentoAsync(
        Guid lancamentoId,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(LancamentoQuery.ListarParticipacoes)
            .Com("lancamentoId", lancamentoId);

        var participacoes = new List<LancamentoParticipacaoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            participacoes.Add(LancamentoMaterializador.LerParticipacao(leitor));
        }

        return participacoes;
    }
}
