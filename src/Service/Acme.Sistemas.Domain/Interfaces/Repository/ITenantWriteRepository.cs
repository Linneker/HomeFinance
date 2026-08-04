using Acme.Sistemas.Domain.Entities.Tenants;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>
/// Escrita de compartilhamento — e as leituras que a decidem, pelo primário.
/// </summary>
public interface ITenantWriteRepository
{
    /// <summary>
    /// Resolve o acesso do usuário a um tenant. É o que a aplicação consulta ANTES de definir
    /// o contexto de RLS — a policy confia no tenant selecionado, então quem valida a seleção
    /// é esta consulta.
    /// </summary>
    Task<TenantDisponivel?> ResolverAcessoAsync(
        Guid usuarioId,
        Guid tenantId,
        CancellationToken cancellationToken);

    /// <summary>Tenant do próprio usuário. Decide a escrita de compartilhamento.</summary>
    Task<Guid?> ObterTenantDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Destinatário existente e ativo, pelo e-mail. Decisão de concessão.</summary>
    Task<Guid?> ObterUsuarioPorEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> ExisteCompartilhamentoVigenteAsync(
        Guid tenantId,
        Guid usuarioDestinatarioId,
        CancellationToken cancellationToken);

    Task ConcederAsync(TenantCompartilhamentoEntity compartilhamento, CancellationToken cancellationToken);

    /// <summary>Revoga a concessão vigente. Devolve falso se não havia o que revogar.</summary>
    Task<bool> RevogarAsync(
        Guid tenantId,
        Guid usuarioDestinatarioId,
        DateTimeOffset revogadoEm,
        CancellationToken cancellationToken);
}
