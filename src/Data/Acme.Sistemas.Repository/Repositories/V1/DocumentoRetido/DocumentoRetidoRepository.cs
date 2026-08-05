using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.DocumentoRetido.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.DocumentoRetido;

/// <inheritdoc cref="IDocumentoRetidoRepository"/>
public sealed class DocumentoRetidoRepository(TransactionManager transacoes)
    : IDocumentoRetidoRepository
{
    public async Task ReterAsync(
        DocumentoRetidoEntity documento,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(DocumentoRetidoCommand.Reter)
            .Com("id", documento.Id)
            .Com("tenantId", documento.TenantId)
            .Com("referencia", documento.Referencia)
            .Com("tipo", documento.Tipo)
            .Com("recebidoEm", documento.CriadoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentoRetidoEntity>> PendentesAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(DocumentoRetidoQuery.Pendentes);

        var pendentes = new List<DocumentoRetidoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            pendentes.Add(new DocumentoRetidoEntity
            {
                Id = leitor.Identificador("id"),
                TenantId = leitor.Identificador("tenant_id"),
                Referencia = leitor.TextoObrigatorio("referencia"),
                Tipo = leitor.TextoObrigatorio("tipo"),
                CriadoEm = leitor.DataHora("recebido_em"),
                ProcessadoEm = leitor.DataHoraOpcional("processado_em"),
            });
        }

        return pendentes;
    }

    public async Task<int> MarcarProcessadosAsync(
        IReadOnlyList<Guid> documentoIds,
        DateTimeOffset quando,
        CancellationToken cancellationToken)
    {
        if (documentoIds.Count == 0)
        {
            return 0;
        }

        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(DocumentoRetidoCommand.MarcarProcessados)
            .Com("ids", documentoIds.ToArray())
            .Com("processadoEm", quando.UtcDateTime);

        var afetadas = await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);

        return afetadas;
    }
}
