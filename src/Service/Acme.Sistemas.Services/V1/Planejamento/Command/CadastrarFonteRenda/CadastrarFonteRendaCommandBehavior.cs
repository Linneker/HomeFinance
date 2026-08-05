using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.CadastrarFonteRenda;

public sealed class CadastrarFonteRendaCommandBehavior(
    ILogger<CadastrarFonteRendaCommandBehavior> logger)
    : IPipelineBehavior<CadastrarFonteRendaCommand, CadastrarFonteRendaCommandResult>
{
    public async Task<CadastrarFonteRendaCommandResult> HandleAsync(
        CadastrarFonteRendaCommand request,
        RequestHandlerDelegate<CadastrarFonteRendaCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            // Sem valor e sem nome: renda é dado sensível e não vai para log.
            logger.LogInformation(
                "Fonte de renda cadastrada com vigência a partir de {Vigencia}.",
                resultado.VigenciaInicio);
        }

        return resultado;
    }
}
