using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Alertas;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Alerta.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Alerta;

public sealed class AlertaReadRepository(TransactionManager transacoes) : IAlertaReadRepository
{
    public async Task<IReadOnlyList<AlertaEntity>> ListarAbertosAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AlertaQuery.ListarAbertos);

        return await LerTodosAsync(comando, cancellationToken);
    }

    internal static async Task<IReadOnlyList<AlertaEntity>> LerTodosAsync(
        DbCommand comando,
        CancellationToken cancellationToken)
    {
        var alertas = new List<AlertaEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            alertas.Add(Ler(leitor));
        }

        return alertas;
    }

    internal static AlertaEntity Ler(DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        TenantId = leitor.Identificador("tenant_id"),
        Tipo = (TipoAlerta)leitor.GetInt16(leitor.GetOrdinal("tipo")),
        ChaveCondicao = leitor.TextoObrigatorio("chave_condicao"),
        Estado = (EstadoAlerta)leitor.GetInt16(leitor.GetOrdinal("estado")),
        Mensagem = leitor.TextoObrigatorio("mensagem"),
        CriadoEm = leitor.DataHora("criado_em"),
        EncerradoEm = leitor.DataHoraOpcional("encerrado_em"),
        DispensadoEm = leitor.DataHoraOpcional("dispensado_em"),
    };
}
