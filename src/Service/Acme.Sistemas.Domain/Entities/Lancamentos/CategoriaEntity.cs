namespace Acme.Sistemas.Domain.Entities.Lancamentos;

/// <summary>Rótulo de agrupamento do gasto. Único por tenant, sem diferenciar caixa.</summary>
public sealed class CategoriaEntity : TenantEntityBase
{
    public required string Nome { get; set; }
}
