using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Services.V1.Alertas;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.AlterarLancamento;

/// <summary>
/// Registro da alteração e reavaliação dos alertas dos meses tocados.
/// </summary>
public sealed class AlterarLancamentoCommandBehavior(
    ILogger<AlterarLancamentoCommandBehavior> logger,
    IAlertaService alertas)
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

            // Duas competências quando a alteração mudou o mês. Reapurar só a nova deixaria a
            // antiga alertada para sempre, com um gasto que já não está lá.
            foreach (var competencia in resultado.CompetenciasAfetadas)
            {
                await alertas.AvaliarCompetenciaAsync(competencia, cancellationToken);
            }

            await alertas.AvaliarCaixaAsync(cancellationToken);
        }

        return resultado;
    }
}
