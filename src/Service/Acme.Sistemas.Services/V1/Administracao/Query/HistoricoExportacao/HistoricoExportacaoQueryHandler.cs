using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Administracao.Query.HistoricoExportacao;

public sealed class HistoricoExportacaoQueryHandler(IAuditoriaReadRepository auditoria)
    : IRequestHandler<HistoricoExportacaoQuery, HistoricoExportacaoQueryResult>
{
    public async Task<HistoricoExportacaoQueryResult> HandleAsync(
        HistoricoExportacaoQuery request,
        CancellationToken cancellationToken)
    {
        var registros = await auditoria.HistoricoExportacaoAsync(request.ContaUsuarioId, cancellationToken);

        var entradas = registros
            .Select(registro => new EntradaHistorico(
                registro.Id,
                registro.Evento,
                registro.AutorId,
                registro.Detalhe,
                registro.CriadoEm))
            .ToList();

        return new HistoricoExportacaoQueryResult(entradas);
    }
}
