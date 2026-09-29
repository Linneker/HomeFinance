using Acme.Sistemas.Domain.Entities.Auditoria;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Auditoria.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Auditoria;

/// <inheritdoc cref="IAuditoriaReadRepository"/>
public sealed class AuditoriaReadRepository(TransactionManager transacoes) : IAuditoriaReadRepository
{
    public async Task<IReadOnlyList<RegistroAuditoriaEntity>> HistoricoExportacaoAsync(
        Guid contaUsuarioId,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AuditoriaQuery.HistoricoExportacao)
            .Com("conta", contaUsuarioId);

        var registros = new List<RegistroAuditoriaEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            var detalheOrdinal = leitor.GetOrdinal("detalhe");

            registros.Add(new RegistroAuditoriaEntity
            {
                Id = leitor.Identificador("id"),
                Evento = (EventoAuditoria)leitor.GetInt16(leitor.GetOrdinal("evento")),
                ContaUsuarioId = contaUsuarioId,
                AutorId = leitor.Identificador("autor_id"),
                Detalhe = leitor.IsDBNull(detalheOrdinal) ? null : leitor.GetString(detalheOrdinal),
                CriadoEm = leitor.DataHora("criado_em"),
            });
        }

        return registros;
    }
}
