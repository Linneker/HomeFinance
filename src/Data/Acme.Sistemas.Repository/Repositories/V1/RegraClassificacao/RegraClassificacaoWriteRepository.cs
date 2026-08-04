using Acme.Sistemas.Domain.Entities.Classificacao;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.RegraClassificacao.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.RegraClassificacao;

/// <summary>
/// Escrita das regras — e a listagem que decide a classificação de um lançamento, pelo primário.
/// </summary>
public sealed class RegraClassificacaoWriteRepository(TransactionManager transacoes)
    : IRegraClassificacaoWriteRepository
{
    public async Task<IReadOnlyList<RegraClassificacaoEntity>> ListarAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(RegraClassificacaoQuery.Listar);

        var regras = new List<RegraClassificacaoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            regras.Add(RegraClassificacaoReadRepository.Ler(leitor));
        }

        return regras;
    }

    public async Task<RegraClassificacaoEntity?> ObterAsync(
        Guid regraId,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(RegraClassificacaoQuery.Obter)
            .Com("id", regraId);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken)
            ? RegraClassificacaoReadRepository.Ler(leitor)
            : null;
    }

    public async Task GravarAsync(RegraClassificacaoEntity regra, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(RegraClassificacaoCommand.Gravar)
            .Com("id", regra.Id)
            .Com("tenantId", regra.TenantId)
            .Com("padrao", regra.Padrao)
            .Com("categoriaId", regra.CategoriaId)
            .Com("criadaEm", regra.CriadoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<bool> AlterarAsync(
        RegraClassificacaoEntity regra,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(RegraClassificacaoCommand.Alterar)
            .Com("id", regra.Id)
            .Com("padrao", regra.Padrao)
            .Com("categoriaId", regra.CategoriaId);

        var afetadas = await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);

        return afetadas > 0;
    }

    public async Task<bool> RemoverAsync(Guid regraId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(RegraClassificacaoCommand.Remover)
            .Com("id", regraId);

        var afetadas = await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);

        return afetadas > 0;
    }
}
