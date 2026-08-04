using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.RemoverLancamento;

public sealed class RemoverLancamentoCommandBehavior(ILogger<RemoverLancamentoCommandBehavior> logger)
    : IPipelineBehavior<RemoverLancamentoCommand, RemoverLancamentoCommandResult>
{
    public async Task<RemoverLancamentoCommandResult> HandleAsync(
        RemoverLancamentoCommand request,
        RequestHandlerDelegate<RemoverLancamentoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Lançamento removido do tenant ativo.");
        }

        return resultado;
    }
}
