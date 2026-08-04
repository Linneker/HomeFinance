using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarRecorrencia;

public sealed class CriarRecorrenciaCommandBehavior(ILogger<CriarRecorrenciaCommandBehavior> logger)
    : IPipelineBehavior<CriarRecorrenciaCommand, CriarRecorrenciaCommandResult>
{
    public async Task<CriarRecorrenciaCommandResult> HandleAsync(
        CriarRecorrenciaCommand request,
        RequestHandlerDelegate<CriarRecorrenciaCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation(
                "Recorrência criada com {Ocorrencias} ocorrência(s) prevista(s).",
                resultado.OcorrenciasGeradas);
        }

        return resultado;
    }
}
