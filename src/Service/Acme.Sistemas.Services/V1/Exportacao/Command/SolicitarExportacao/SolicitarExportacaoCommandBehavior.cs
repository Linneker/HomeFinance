using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.SolicitarExportacao;

public sealed class SolicitarExportacaoCommandBehavior(
    ILogger<SolicitarExportacaoCommandBehavior> logger)
    : IPipelineBehavior<SolicitarExportacaoCommand, SolicitarExportacaoCommandResult>
{
    public async Task<SolicitarExportacaoCommandResult> HandleAsync(
        SolicitarExportacaoCommand request,
        RequestHandlerDelegate<SolicitarExportacaoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Exportação solicitada no formato {Formato}.", request.Formato);
        }

        return resultado;
    }
}
