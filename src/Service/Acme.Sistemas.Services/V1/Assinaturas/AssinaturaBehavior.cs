using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Assinaturas;

/// <summary>
/// Suspende os recursos derivados enquanto a assinatura não estiver vigente.
/// </summary>
/// <remarks>
/// Derivado é o que o produto <em>produz</em> sobre os dados: análise, alerta, orientação,
/// compartilhamento. Registrar e ler os próprios lançamentos ficam de fora — reter o dado que a
/// pessoa digitou seria usá-lo como refém, e excluir a conta continua funcionando porque
/// ninguém deve ser obrigado a pagar para sair.
/// <para>
/// Independente do <c>OnboardingBehavior</c>: um restringe registro, o outro restringe análise.
/// Com onboarding pendente e assinatura suspensa, tentar lançar responde sobre o onboarding —
/// que é o que de fato barra aquele pedido —, e não sobre a assinatura.
/// </para>
/// </remarks>
public sealed class AssinaturaBehavior<TRequest, TResult>(
    IAssinaturaService assinaturas,
    ILogger<AssinaturaBehavior<TRequest, TResult>> logger)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : IRequest<TResult>, IRecursoDerivado
    where TResult : IResultadoRecusavel<TResult>
{
    public const string CodigoAssinaturaInativa = "assinatura_inativa";

    public async Task<TResult> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        var situacao = await assinaturas.SituacaoAtualAsync(cancellationToken);

        if (situacao.Vigente)
        {
            return await next();
        }

        logger.LogInformation(
            "Recurso derivado indisponível: assinatura em {Situacao}.", situacao.Situacao);

        return TResult.Recusar(
            new Error(
                CodigoAssinaturaInativa,
                "Este recurso depende de assinatura ativa."));
    }
}
