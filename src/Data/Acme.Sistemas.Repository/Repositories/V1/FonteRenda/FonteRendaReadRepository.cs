using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.FonteRenda.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.FonteRenda;

public sealed class FonteRendaReadRepository(TransactionManager transacoes) : IFonteRendaReadRepository
{
    public async Task<IReadOnlyList<FonteRendaEntity>> ListarAsync(
        DateOnly? competencia,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(FonteRendaQuery.Listar(competencia is not null));

        if (competencia is not null)
        {
            comando.Com("competencia", competencia.Value);
        }

        return await LerTodasAsync(comando, cancellationToken);
    }

    internal static async Task<IReadOnlyList<FonteRendaEntity>> LerTodasAsync(
        DbCommand comando,
        CancellationToken cancellationToken)
    {
        var fontes = new List<FonteRendaEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            fontes.Add(Ler(leitor));
        }

        return fontes;
    }

    internal static FonteRendaEntity Ler(DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        TenantId = leitor.Identificador("tenant_id"),
        Nome = leitor.TextoObrigatorio("nome"),
        Valor = leitor.GetDecimal(leitor.GetOrdinal("valor")),
        DiaRecebimento = leitor.GetInt16(leitor.GetOrdinal("dia_recebimento")),
        VigenciaInicio = leitor.Data("vigencia_inicio"),
        VigenciaFim = leitor.DataOpcional("vigencia_fim"),
        CriadoEm = leitor.DataHora("criado_em"),
    };
}
