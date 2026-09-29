using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.ListarCompartilhamentos;

/// <summary>Sem cache: um acesso revogado tem de sumir da lista no mesmo instante.</summary>
public sealed class ListarCompartilhamentosQueryBehavior
    : IPipelineBehavior<ListarCompartilhamentosQuery, ListarCompartilhamentosQueryResult>
{
    public Task<ListarCompartilhamentosQueryResult> HandleAsync(
        ListarCompartilhamentosQuery request,
        RequestHandlerDelegate<ListarCompartilhamentosQueryResult> next,
        CancellationToken cancellationToken) => next();
}
