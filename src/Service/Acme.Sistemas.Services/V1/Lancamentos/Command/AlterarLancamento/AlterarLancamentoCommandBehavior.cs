using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.AlterarLancamento;

public sealed class AlterarLancamentoCommandBehavior(ILogger<AlterarLancamentoCommandBehavior> logger)
    : IPipelineBehavior<AlterarLancamentoCommand, AlterarLancamentoCommandResult>
{
    public async Task<AlterarLancamentoCommandResult> HandleAsync(
        AlterarLancamentoCommand request,
        RequestHandlerDelegate<AlterarLancamentoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation(
                "Lançamento alterado; {Meses} competência(s) a reapurar.",
                resultado.CompetenciasAfetadas.Count);
        }

        return resultado;
    }
}
