namespace Acme.Sistemas.Domain.Entities;

/// <summary>Campos comuns a todas as entidades persistidas.</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}

/// <summary>Entidade que pertence a um tenant. Toda tabela correspondente tem RLS forçada.</summary>
public abstract class TenantEntityBase : BaseEntity
{
    public Guid TenantId { get; set; }
}
