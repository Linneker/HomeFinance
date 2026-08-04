namespace Acme.Sistemas.Domain.Entities.Lancamentos;

/// <summary>
/// A quem o valor do lançamento é atribuído, e em que proporção.
/// </summary>
/// <remarks>
/// Nesta fase existe sempre exatamente uma linha a 100% para o dono do tenant, o que a faz
/// parecer supérflua. Ela existe agora porque todos os relatórios apuram valor por pessoa a
/// partir daqui, e não do <c>tenant_id</c> do lançamento: quando o rateio entre pessoas
/// chegar, passa a ser inserção de linhas somando 100 e nenhuma consulta de relatório muda.
/// <para>
/// O custo é um JOIN a mais hoje. O retorno é não reescrever relatório com cliente em produção.
/// </para>
/// </remarks>
public sealed class LancamentoParticipacaoEntity : TenantEntityBase
{
    public Guid LancamentoId { get; set; }

    public Guid UsuarioId { get; set; }

    public decimal Percentual { get; set; }

    /// <summary>Participação integral do dono. O único formato desta fase.</summary>
    public const decimal PercentualIntegral = 100m;
}
