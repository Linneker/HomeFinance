using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Services.V1.Alertas;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.RemoverLancamento;

/// <summary>
/// Registro da remoção e reavaliação dos alertas — é o caminho pelo qual um estouro se corrige.
/// </summary>
public sealed class RemoverLancamentoCommandBehavior(
    ILogger<RemoverLancamentoCommandBehavior> logger,
    IAlertaService alertas)
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

            await alertas.AvaliarCompetenciaAsync(
                resultado.CompetenciaAfetada!.Value, cancellationToken);

            await alertas.AvaliarCaixaAsync(cancellationToken);
        }

        return resultado;
    }
}
