using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.TenantsDisponiveis;

/// <summary>
/// Lista os tenants que o usuário pode selecionar: o próprio e os recebidos por compartilhamento.
/// </summary>
/// <remarks>
/// Query de leitura do usuário, então usa o repositório de LEITURA. Não confundir com a
/// resolução do middleware, que decide o contexto de RLS e por isso vai ao primário.
/// </remarks>
public sealed class TenantsDisponiveisQueryHandler(ITenantReadRepository tenants)
    : IRequestHandler<TenantsDisponiveisQuery, TenantsDisponiveisQueryResult>
{
    public async Task<TenantsDisponiveisQueryResult> HandleAsync(
        TenantsDisponiveisQuery request,
        CancellationToken cancellationToken)
    {
        if (TenantsDisponiveisQueryValidation.Validar(request).Count > 0)
        {
            return new TenantsDisponiveisQueryResult([]);
        }

        var disponiveis = await tenants.ObterDisponiveisAsync(request.UsuarioId, cancellationToken);

        return new TenantsDisponiveisQueryResult(
            [.. disponiveis.Select(t => new TenantSelecionavel(t.TenantId, t.EhProprio, t.SomenteLeitura))]);
    }
}
