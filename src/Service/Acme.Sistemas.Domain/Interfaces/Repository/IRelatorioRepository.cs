using Acme.Sistemas.Domain.Entities.Relatorios;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <param name="PorCaixa">Falso é competência, a visão primária do produto.</param>
/// <param name="Ate">Inclusiva: o chamador informa o último dia do mês.</param>
public sealed record PeriodoDoRelatorio(DateOnly De, DateOnly Ate, bool PorCaixa);

/// <summary>
/// Leituras dos relatórios mensais.
/// </summary>
/// <remarks>
/// Todas agregam a partir de <c>lancamento_participacao</c>, nunca do <c>tenant_id</c> do
/// lançamento. É o que mantém o relatório correto quando a divisão entre pessoas entrar: a
/// consulta já soma a fatia, não o valor cheio.
/// </remarks>
public interface IRelatorioReadRepository
{
    /// <summary>Extrato do período, uma linha por lançamento, com o valor apurado.</summary>
    Task<IReadOnlyList<LancamentoApuradoEntity>> ExtratoAsync(
        PeriodoDoRelatorio periodo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Total apurado por categoria no período, excluídas as ocorrências ainda previstas.
    /// </summary>
    /// <remarks>
    /// Previsto é projeção, não gasto. Somá-lo ao realizado faria uma recorrência futura
    /// consumir orçamento antes de o dinheiro sair, e a categoria apareceria estourada num mês
    /// em que a pessoa ainda não gastou nada.
    /// </remarks>
    Task<IReadOnlyList<TotalPorCategoria>> TotaisPorCategoriaAsync(
        PeriodoDoRelatorio periodo,
        CancellationToken cancellationToken);
}
