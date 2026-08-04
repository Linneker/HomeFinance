using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.EncerrarRecorrencia;

public sealed class EncerrarRecorrenciaCommandBehavior(ILogger<EncerrarRecorrenciaCommandBehavior> logger)
    : IPipelineBehavior<EncerrarRecorrenciaCommand, EncerrarRecorrenciaCommandResult>
{
    public async Task<EncerrarRecorrenciaCommandResult> HandleAsync(
        EncerrarRecorrenciaCommand request,
        RequestHandlerDelegate<EncerrarRecorrenciaCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Recorrência encerrada a partir de {Corte}.", resultado.APartirDe);
        }

        return resultado;
    }
}
