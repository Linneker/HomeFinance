using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.AlterarRegra;

public sealed class AlterarRegraCommandBehavior(ILogger<AlterarRegraCommandBehavior> logger)
    : IPipelineBehavior<AlterarRegraCommand, AlterarRegraCommandResult>
{
    public async Task<AlterarRegraCommandResult> HandleAsync(
        AlterarRegraCommand request,
        RequestHandlerDelegate<AlterarRegraCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Regra de classificação alterada no tenant ativo.");
        }

        return resultado;
    }
}
