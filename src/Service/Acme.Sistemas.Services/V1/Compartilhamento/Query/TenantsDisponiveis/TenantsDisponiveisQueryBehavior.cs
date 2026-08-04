using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.TenantsDisponiveis;

/// <summary>
/// Sem cache: a lista muda no instante em que alguém revoga um compartilhamento, e servir
/// valor cacheado manteria visível um acesso já encerrado.
/// </summary>
public sealed class TenantsDisponiveisQueryBehavior
    : IPipelineBehavior<TenantsDisponiveisQuery, TenantsDisponiveisQueryResult>
{
    public Task<TenantsDisponiveisQueryResult> HandleAsync(
        TenantsDisponiveisQuery request,
        RequestHandlerDelegate<TenantsDisponiveisQueryResult> next,
        CancellationToken cancellationToken) => next();
}
