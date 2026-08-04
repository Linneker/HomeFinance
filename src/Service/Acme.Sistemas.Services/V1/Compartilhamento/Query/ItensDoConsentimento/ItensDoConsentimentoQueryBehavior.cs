using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.ItensDoConsentimento;

/// <summary>
/// Sem cache, apesar de a lista ser estática hoje.
/// </summary>
/// <remarks>
/// O dia em que um item passar a depender do plano da conta, um cache colocado aqui
/// devolveria a lista de outro titular. O custo de montar a lista é nulo; o de acertar isso
/// depois, não.
/// </remarks>
public sealed class ItensDoConsentimentoQueryBehavior
    : IPipelineBehavior<ItensDoConsentimentoQuery, ItensDoConsentimentoQueryResult>
{
    public Task<ItensDoConsentimentoQueryResult> HandleAsync(
        ItensDoConsentimentoQuery request,
        RequestHandlerDelegate<ItensDoConsentimentoQueryResult> next,
        CancellationToken cancellationToken) => next();
}
