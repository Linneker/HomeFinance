using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Lancamento;

public sealed class LancamentoReadRepository(TransactionManager transacoes) : ILancamentoReadRepository
{
    public async Task<IReadOnlyList<LancamentoEntity>> ListarAsync(
        FiltroDeLancamento filtro,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(LancamentoQuery.Listar(filtro.PorCaixa, filtro.CategoriaId is not null))
            .Com("de", filtro.De)
            .Com("ate", filtro.Ate);

        if (filtro.CategoriaId is not null)
        {
            comando.Com("categoriaId", filtro.CategoriaId);
        }

        var lancamentos = new List<LancamentoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            lancamentos.Add(LancamentoMaterializador.Ler(leitor));
        }

        return lancamentos;
    }

    public async Task<LancamentoEntity?> ObterAsync(Guid lancamentoId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(LancamentoQuery.Obter)
            .Com("id", lancamentoId);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken) ? LancamentoMaterializador.Ler(leitor) : null;
    }
}
