using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Identidade.Command.RegistrarConsentimento;

/// <summary>
/// Registra o aceite também no log de aplicação.
/// </summary>
/// <remarks>
/// Duplicidade proposital: se a linha do banco for perdida ou contestada, o log com data e
/// versão é evidência corroborante. Sem identificar o titular — documento e versão bastam.
/// </remarks>
public sealed class RegistrarConsentimentoCommandBehavior(
    ILogger<RegistrarConsentimentoCommandBehavior> logger)
    : IPipelineBehavior<RegistrarConsentimentoCommand, RegistrarConsentimentoCommandResult>
{
    public async Task<RegistrarConsentimentoCommandResult> HandleAsync(
        RegistrarConsentimentoCommand request,
        RequestHandlerDelegate<RegistrarConsentimentoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation(
                "Consentimento registrado: {Documento} versão {Versao}.",
                request.Documento,
                request.Versao);
        }

        return resultado;
    }
}
