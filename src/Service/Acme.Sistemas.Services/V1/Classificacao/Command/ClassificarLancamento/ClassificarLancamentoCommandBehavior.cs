using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Services.V1.Alertas;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.ClassificarLancamento;

/// <summary>
/// Registra a correção e se ela virou regra.
/// </summary>
/// <remarks>
/// A proporção entre correções e regras criadas é o sinal de que a classificação automática
/// está funcionando: muita correção sem regra significa que a oferta não está convencendo,
/// e a pessoa vai classificar a mesma compra todo mês.
/// </remarks>
public sealed class ClassificarLancamentoCommandBehavior(
    ILogger<ClassificarLancamentoCommandBehavior> logger,
    IAlertaService alertas)
    : IPipelineBehavior<ClassificarLancamentoCommand, ClassificarLancamentoCommandResult>
{
    public async Task<ClassificarLancamentoCommandResult> HandleAsync(
        ClassificarLancamentoCommand request,
        RequestHandlerDelegate<ClassificarLancamentoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation(
                "Lançamento classificado manualmente; regra criada: {CriouRegra}.",
                resultado.RegraId is not null);

            // Classificar move valor entre duas categorias do mesmo mês: uma pode ter acabado
            // de estourar e a outra, de voltar para dentro do plano. Por isso a avaliação
            // percorre o mês inteiro, e não só a categoria escolhida.
            await alertas.AvaliarCompetenciaAsync(
                resultado.CompetenciaAfetada!.Value, cancellationToken);
        }

        return resultado;
    }
}
