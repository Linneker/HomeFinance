using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Orcamento.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Orcamento;

public sealed class OrcamentoReadRepository(TransactionManager transacoes) : IOrcamentoReadRepository
{
    public async Task<IReadOnlyList<OrcamentoMensalEntity>> ListarDoMesAsync(
        DateOnly competencia,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(OrcamentoQuery.ListarDoMes)
            .Com("competencia", competencia);

        return await LerTodosAsync(comando, cancellationToken);
    }

    internal static async Task<IReadOnlyList<OrcamentoMensalEntity>> LerTodosAsync(
        DbCommand comando,
        CancellationToken cancellationToken)
    {
        var orcamentos = new List<OrcamentoMensalEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            orcamentos.Add(Ler(leitor));
        }

        return orcamentos;
    }

    internal static OrcamentoMensalEntity Ler(DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        TenantId = leitor.Identificador("tenant_id"),
        CategoriaId = leitor.Identificador("categoria_id"),
        Competencia = leitor.Data("competencia"),
        ValorPlanejado = leitor.GetDecimal(leitor.GetOrdinal("valor_planejado")),
        CriadoEm = leitor.DataHora("criado_em"),
    };
}
