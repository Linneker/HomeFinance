using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.RemoverRegra;

public sealed class RemoverRegraCommandBehavior(ILogger<RemoverRegraCommandBehavior> logger)
    : IPipelineBehavior<RemoverRegraCommand, RemoverRegraCommandResult>
{
    public async Task<RemoverRegraCommandResult> HandleAsync(
        RemoverRegraCommand request,
        RequestHandlerDelegate<RemoverRegraCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Regra de classificação removida do tenant ativo.");
        }

        return resultado;
    }
}
