using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Lancamentos.Query.Lancamentos;

/// <summary>
/// Sem cache. O extrato do mês corrente muda a cada lançamento, e a tela que não exibisse o
/// gasto recém-gravado levaria a pessoa a digitá-lo de novo — que é exatamente o que a
/// detecção de duplicidade existe para evitar.
/// </summary>
public sealed class LancamentosQueryBehavior : IPipelineBehavior<LancamentosQuery, LancamentosQueryResult>
{
    public Task<LancamentosQueryResult> HandleAsync(
        LancamentosQuery request,
        RequestHandlerDelegate<LancamentosQueryResult> next,
        CancellationToken cancellationToken) => next();
}
