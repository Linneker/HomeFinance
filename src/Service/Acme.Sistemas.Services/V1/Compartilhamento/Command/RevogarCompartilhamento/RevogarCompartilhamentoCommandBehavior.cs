using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Command.RevogarCompartilhamento;

/// <summary>
/// Registra a revogação. É o par do log de concessão: sem ele, a auditoria mostraria acessos
/// concedidos e nunca encerrados.
/// </summary>
public sealed class RevogarCompartilhamentoCommandBehavior(
    ILogger<RevogarCompartilhamentoCommandBehavior> logger)
    : IPipelineBehavior<RevogarCompartilhamentoCommand, RevogarCompartilhamentoCommandResult>
{
    public async Task<RevogarCompartilhamentoCommandResult> HandleAsync(
        RevogarCompartilhamentoCommand request,
        RequestHandlerDelegate<RevogarCompartilhamentoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Compartilhamento revogado no tenant do titular.");
        }

        return resultado;
    }
}
