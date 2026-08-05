using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.AlterarFonteRenda;

public sealed class AlterarFonteRendaCommandBehavior(
    ILogger<AlterarFonteRendaCommandBehavior> logger)
    : IPipelineBehavior<AlterarFonteRendaCommand, AlterarFonteRendaCommandResult>
{
    public async Task<AlterarFonteRendaCommandResult> HandleAsync(
        AlterarFonteRendaCommand request,
        RequestHandlerDelegate<AlterarFonteRendaCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation(
                "Fonte de renda alterada a partir de {Vigencia}. Nova vigência: {Nova}.",
                resultado.VigenciaInicio,
                resultado.AbriuNovaVigencia);
        }

        return resultado;
    }
}
