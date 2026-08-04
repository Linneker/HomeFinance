using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Lancamentos.Query.Categorias;

/// <summary>
/// Sem cache. Um cache por tenant aqui teria que ser invalidado a cada categoria criada, e a
/// tela de lançamento que não enxergasse a categoria recém-criada empurraria a pessoa a criar
/// a mesma de novo.
/// </summary>
public sealed class CategoriasQueryBehavior : IPipelineBehavior<CategoriasQuery, CategoriasQueryResult>
{
    public Task<CategoriasQueryResult> HandleAsync(
        CategoriasQuery request,
        RequestHandlerDelegate<CategoriasQueryResult> next,
        CancellationToken cancellationToken) => next();
}
