using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Administracao.Query.ListarContas;

public sealed class ListarContasQueryBehavior
    : IPipelineBehavior<ListarContasQuery, ListarContasQueryResult>
{
    public Task<ListarContasQueryResult> HandleAsync(
        ListarContasQuery request,
        RequestHandlerDelegate<ListarContasQueryResult> next,
        CancellationToken cancellationToken) => next();
}
