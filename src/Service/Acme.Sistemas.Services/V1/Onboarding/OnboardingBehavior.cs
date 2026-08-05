using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Onboarding;

/// <summary>
/// Impede a escrita de dado financeiro enquanto houver requisito bloqueante pendente.
/// </summary>
/// <remarks>
/// Genérico e fechado por restrição: vale para todo Command marcado com
/// <see cref="IEscritaFinanceira"/> cujo resultado saiba se construir como recusa. Um Command
/// financeiro novo passa a ser coberto ao ganhar a marca — não há lista a manter em outro
/// arquivo, e esquecer a marca aparece no review do próprio Command.
/// <para>
/// Só escrita. Nenhuma Query passa por aqui, e é isso que sustenta a navegação liberada desde o
/// primeiro acesso: as telas abrem, mostram estado vazio e dizem o que falta, em vez de
/// devolver erro para quem ainda nem começou.
/// </para>
/// </remarks>
public sealed class OnboardingBehavior<TRequest, TResult>(
    IOnboardingService onboarding,
    IContextoRequisicao contexto,
    ILogger<OnboardingBehavior<TRequest, TResult>> logger)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : IRequest<TResult>, IEscritaFinanceira
    where TResult : IResultadoRecusavel<TResult>
{
    public const string CodigoOnboardingPendente = "onboarding_pendente";

    public async Task<TResult> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        // Sem tenant, ou em tenant de terceiro, o onboarding não é a barreira certa: quem
        // recusa é a RLS, e recusar aqui daria à pessoa uma mensagem sobre o próprio
        // onboarding quando o problema é que ela não pode escrever naquele espaço.
        if (contexto.TenantId is null || contexto.SomenteLeitura)
        {
            return await next();
        }

        var progresso = await onboarding.ProgressoAsync(cancellationToken);

        if (progresso.Liberado)
        {
            return await next();
        }

        var pendente = progresso.Bloqueios[0];

        logger.LogInformation(
            "Escrita recusada: onboarding pendente em {Requisito}.", pendente.Codigo);

        // A mensagem nomeia o requisito. Recusar sem dizer qual deixaria a pessoa procurando na
        // tela inteira o que destrava a ação que ela acabou de tentar.
        return TResult.Recusar(
            new Error(CodigoOnboardingPendente, pendente.Descricao, pendente.Codigo));
    }
}
