using Acme.Sistemas.Core.Mediators;
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
    ILogger<ClassificarLancamentoCommandBehavior> logger)
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
        }

        return resultado;
    }
}
