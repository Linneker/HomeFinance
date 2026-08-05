using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Services.V1.Alertas;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;

/// <summary>
/// Registro da gravação e reavaliação dos alertas que ela pode ter mudado.
/// </summary>
/// <remarks>
/// O aviso de possível duplicidade é registrado à parte do sucesso porque não é erro: é o
/// caminho normal de quem digita duas vezes o mesmo gasto. Contá-lo junto das falhas
/// esconderia, na operação, se o aviso está ajudando ou só atrapalhando.
/// <para>
/// A avaliação dos alertas fica no behavior, e não no handler: é efeito da gravação ter dado
/// certo, não parte de gravar. No handler, cada Command precisaria repetir a chamada e lembrar
/// de não fazê-la nos caminhos de recusa.
/// </para>
/// </remarks>
public sealed class CriarLancamentoCommandBehavior(
    ILogger<CriarLancamentoCommandBehavior> logger,
    IAlertaService alertas)
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

            // Competência do request, e não do resultado: a compra parcelada gera N linhas com
            // a mesma competência, e é essa única que precisa ser reapurada.
            await alertas.AvaliarCompetenciaAsync(
                request.DataCompetencia!.Value, cancellationToken);

            // A parcela desloca dinheiro para meses à frente: o caixa muda mesmo quando a
            // competência continua dentro do plano.
            await alertas.AvaliarCaixaAsync(cancellationToken);
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
