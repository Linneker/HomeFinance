using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Classificacao.Query.Regras;

/// <summary>
/// Sem cache. As regras são por tenant, e um cache mal escopado aqui mostraria as regras de
/// um cliente na tela de outro — a lista revela onde a pessoa compra.
/// </summary>
public sealed class RegrasQueryBehavior : IPipelineBehavior<RegrasQuery, RegrasQueryResult>
{
    public Task<RegrasQueryResult> HandleAsync(
        RegrasQuery request,
        RequestHandlerDelegate<RegrasQueryResult> next,
        CancellationToken cancellationToken) => next();
}
