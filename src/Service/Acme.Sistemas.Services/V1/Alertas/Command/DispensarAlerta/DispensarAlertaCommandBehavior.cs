using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Alertas.Command.DispensarAlerta;

public sealed class DispensarAlertaCommandBehavior(
    ILogger<DispensarAlertaCommandBehavior> logger)
    : IPipelineBehavior<DispensarAlertaCommand, DispensarAlertaCommandResult>
{
    public async Task<DispensarAlertaCommandResult> HandleAsync(
        DispensarAlertaCommand request,
        RequestHandlerDelegate<DispensarAlertaCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Alerta dispensado pelo titular do tenant ativo.");
        }

        return resultado;
    }
}
