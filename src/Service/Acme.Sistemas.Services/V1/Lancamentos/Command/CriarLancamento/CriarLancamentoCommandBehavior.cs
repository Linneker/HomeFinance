using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;

/// <summary>
/// Registro da gravação.
/// </summary>
/// <remarks>
/// O aviso de possível duplicidade é registrado à parte do sucesso porque não é erro: é o
/// caminho normal de quem digita duas vezes o mesmo gasto. Contá-lo junto das falhas
/// esconderia, na operação, se o aviso está ajudando ou só atrapalhando.
/// </remarks>
public sealed class CriarLancamentoCommandBehavior(ILogger<CriarLancamentoCommandBehavior> logger)
    : IPipelineBehavior<CriarLancamentoCommand, CriarLancamentoCommandResult>
{
    public async Task<CriarLancamentoCommandResult> HandleAsync(
        CriarLancamentoCommand request,
        RequestHandlerDelegate<CriarLancamentoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation(
                "Lançamento gravado com {Linhas} linha(s).", resultado.LancamentoIds.Count);
        }
        else if (resultado.AguardandoConfirmacaoDeDuplicidade)
        {
            logger.LogInformation(
                "Possível duplicidade: {Semelhantes} lançamento(s) semelhante(s).",
                resultado.SemelhantesEncontrados);
        }

        return resultado;
    }
}
