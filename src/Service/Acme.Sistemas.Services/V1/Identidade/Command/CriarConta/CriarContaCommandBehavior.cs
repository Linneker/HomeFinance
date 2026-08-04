using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Identidade.Command.CriarConta;

/// <summary>
/// Log da criação de conta sem expor dado pessoal.
/// </summary>
/// <remarks>
/// O e-mail nunca entra no log: log agregado é copiado, exportado e consultado por gente que
/// não deveria ver a base de clientes. Registra-se o resultado, não o titular.
/// </remarks>
public sealed class CriarContaCommandBehavior(ILogger<CriarContaCommandBehavior> logger)
    : IPipelineBehavior<CriarContaCommand, CriarContaCommandResult>
{
    public async Task<CriarContaCommandResult> HandleAsync(
        CriarContaCommand request,
        RequestHandlerDelegate<CriarContaCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Conta criada. Tenant {TenantId}.", resultado.TenantId);
        }
        else
        {
            logger.LogInformation(
                "Criação de conta recusada: {Codigos}.",
                string.Join(", ", resultado.Erros.Select(erro => erro.Codigo).Distinct()));
        }

        return resultado;
    }
}
