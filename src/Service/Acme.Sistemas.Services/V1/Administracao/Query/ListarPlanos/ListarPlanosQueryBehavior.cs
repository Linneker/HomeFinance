using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Administracao.Query.ListarPlanos;

public sealed class ListarPlanosQueryBehavior
    : IPipelineBehavior<ListarPlanosQuery, ListarPlanosQueryResult>
{
    public Task<ListarPlanosQueryResult> HandleAsync(
        ListarPlanosQuery request,
        RequestHandlerDelegate<ListarPlanosQueryResult> next,
        CancellationToken cancellationToken) => next();
}
