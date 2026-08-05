using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.RemoverFonteRenda;

public sealed class RemoverFonteRendaCommandBehavior(
    ILogger<RemoverFonteRendaCommandBehavior> logger)
    : IPipelineBehavior<RemoverFonteRendaCommand, RemoverFonteRendaCommandResult>
{
    public async Task<RemoverFonteRendaCommandResult> HandleAsync(
        RemoverFonteRendaCommand request,
        RequestHandlerDelegate<RemoverFonteRendaCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Fonte de renda removida do tenant ativo.");
        }

        return resultado;
    }
}
