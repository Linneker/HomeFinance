using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento.Query;
using Acme.Sistemas.Repository.Repositories.V1.Recorrencia.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Recorrencia;

public sealed class RecorrenciaReadRepository(TransactionManager transacoes) : IRecorrenciaReadRepository
{
    public async Task<IReadOnlyList<RecorrenciaEntity>> ListarAtivasAsync(CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(RecorrenciaQuery.ListarAtivas);

        var recorrencias = new List<RecorrenciaEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            recorrencias.Add(Ler(leitor));
        }

        return recorrencias;
    }

    internal static RecorrenciaEntity Ler(DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        TenantId = leitor.Identificador("tenant_id"),
        Descricao = leitor.TextoObrigatorio("descricao"),
        Valor = leitor.GetDecimal(leitor.GetOrdinal("valor")),
        DiaDoMes = leitor.GetInt16(leitor.GetOrdinal("dia_do_mes")),
        CategoriaId = leitor.IdentificadorOpcional("categoria_id"),
        Inicio = leitor.Data("inicio"),
        EncerradaEm = leitor.DataHoraOpcional("encerrada_em"),
        CriadoEm = leitor.DataHora("criado_em"),
    };
}
