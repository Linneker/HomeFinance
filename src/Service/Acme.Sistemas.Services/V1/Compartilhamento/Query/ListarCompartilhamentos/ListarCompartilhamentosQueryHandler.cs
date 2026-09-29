using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.ListarCompartilhamentos;

/// <summary>
/// Lista os compartilhamentos vigentes concedidos pelo tenant. Só os não revogados: a revogação
/// é marcação (mantém o histórico), mas a tela mostra quem tem acesso agora.
/// </summary>
public sealed class ListarCompartilhamentosQueryHandler(ITenantReadRepository tenants)
    : IRequestHandler<ListarCompartilhamentosQuery, ListarCompartilhamentosQueryResult>
{
    public async Task<ListarCompartilhamentosQueryResult> HandleAsync(
        ListarCompartilhamentosQuery request,
        CancellationToken cancellationToken)
    {
        if (ListarCompartilhamentosQueryValidation.Validar(request).Count > 0)
        {
            return new ListarCompartilhamentosQueryResult([]);
        }

        var concedidos = await tenants.ObterCompartilhamentosConcedidosAsync(request.TenantId, cancellationToken);

        return new ListarCompartilhamentosQueryResult(
            [.. concedidos
                .Where(c => c.Vigente)
                .Select(c => new CompartilhamentoConcedido(c.UsuarioDestinatarioId, c.IncluiScr, c.ConcedidoEm))]);
    }
}
