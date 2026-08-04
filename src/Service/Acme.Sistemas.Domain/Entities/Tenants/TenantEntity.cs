namespace Acme.Sistemas.Domain.Entities.Tenants;

/// <summary>Espaço de dados de uma pessoa. Um por conta, criado junto com ela.</summary>
public sealed class TenantEntity : BaseEntity
{
    public Guid UsuarioId { get; set; }
}
