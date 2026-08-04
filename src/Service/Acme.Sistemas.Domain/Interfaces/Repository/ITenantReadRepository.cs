using Acme.Sistemas.Domain.Entities.Tenants;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

public interface ITenantReadRepository
{
    /// <summary>Tenants que o usuário pode selecionar: o próprio e os compartilhados com ele.</summary>
    Task<IReadOnlyList<TenantDisponivel>> ObterDisponiveisAsync(
        Guid usuarioId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantCompartilhamentoEntity>> ObterCompartilhamentosConcedidosAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
}
