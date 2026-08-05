namespace Acme.Sistemas.Domain.Entities.Planejamento;

/// <summary>
/// Quanto a pessoa pretende gastar em uma categoria num mês.
/// </summary>
/// <remarks>
/// A competência é sempre o primeiro dia do mês. É a mesma decisão da tabela: guardar o mês
/// como data em vez do texto <c>"2026-08"</c> mantém a ordenação correta na virada de ano e
/// permite comparar com a competência dos lançamentos sem conversão.
/// </remarks>
public sealed class OrcamentoMensalEntity : TenantEntityBase
{
    public Guid CategoriaId { get; set; }

    /// <summary>Primeiro dia do mês orçado.</summary>
    public DateOnly Competencia { get; set; }

    public decimal ValorPlanejado { get; set; }
}
