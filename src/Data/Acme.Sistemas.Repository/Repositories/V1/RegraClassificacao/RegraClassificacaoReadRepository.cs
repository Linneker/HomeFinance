using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Classificacao;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.RegraClassificacao.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.RegraClassificacao;

public sealed class RegraClassificacaoReadRepository(TransactionManager transacoes)
    : IRegraClassificacaoReadRepository
{
    public async Task<IReadOnlyList<RegraClassificacaoEntity>> ListarAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(RegraClassificacaoQuery.Listar);

        var regras = new List<RegraClassificacaoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            regras.Add(Ler(leitor));
        }

        return regras;
    }

    internal static RegraClassificacaoEntity Ler(DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        TenantId = leitor.Identificador("tenant_id"),
        Padrao = leitor.TextoObrigatorio("padrao"),
        CategoriaId = leitor.Identificador("categoria_id"),
        CriadoEm = leitor.DataHora("criada_em"),
    };
}
