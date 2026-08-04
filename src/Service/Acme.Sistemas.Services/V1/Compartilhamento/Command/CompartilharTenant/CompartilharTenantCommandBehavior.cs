using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;

/// <summary>
/// Registra a concessão. Sem e-mail: quem compartilhou com quem é dado de relacionamento, e o
/// log agregado não é lugar para ele. O identificador do compartilhamento basta para auditar.
/// </summary>
public sealed class CompartilharTenantCommandBehavior(
    ILogger<CompartilharTenantCommandBehavior> logger)
    : IPipelineBehavior<CompartilharTenantCommand, CompartilharTenantCommandResult>
{
    public async Task<CompartilharTenantCommandResult> HandleAsync(
        CompartilharTenantCommand request,
        RequestHandlerDelegate<CompartilharTenantCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation(
                "Compartilhamento {Id} concedido. Inclui SCR: {IncluiScr}.",
                resultado.CompartilhamentoId,
                request.IncluiScr);
        }

        return resultado;
    }
}
