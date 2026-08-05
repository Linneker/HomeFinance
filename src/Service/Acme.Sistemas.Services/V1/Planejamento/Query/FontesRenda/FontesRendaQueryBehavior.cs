using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Planejamento.Query.FontesRenda;

/// <summary>
/// Sem cache. Renda é dado do tenant e um cache mal escopado mostraria o salário de um cliente
/// na tela de outro.
/// </summary>
public sealed class FontesRendaQueryBehavior
    : IPipelineBehavior<FontesRendaQuery, FontesRendaQueryResult>
{
    public Task<FontesRendaQueryResult> HandleAsync(
        FontesRendaQuery request,
        RequestHandlerDelegate<FontesRendaQueryResult> next,
        CancellationToken cancellationToken) => next();
}
