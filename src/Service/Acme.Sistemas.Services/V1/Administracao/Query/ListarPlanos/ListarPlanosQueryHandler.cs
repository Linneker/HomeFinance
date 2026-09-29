using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Administracao.Query.ListarPlanos;

public sealed class ListarPlanosQueryHandler(IPlanoReadRepository planos)
    : IRequestHandler<ListarPlanosQuery, ListarPlanosQueryResult>
{
    public async Task<ListarPlanosQueryResult> HandleAsync(
        ListarPlanosQuery request,
        CancellationToken cancellationToken)
    {
        var catalogo = await planos.ListarTodosAsync(cancellationToken);

        var resumos = catalogo
            .Select(plano => new PlanoResumo(
                plano.Id,
                plano.Codigo,
                plano.Nome,
                plano.CotaCompartilhamentos,
                plano.PrecoMensal,
                plano.Ativo))
            .ToList();

        return new ListarPlanosQueryResult(resumos);
    }
}
