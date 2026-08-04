using Acme.Sistemas.Domain.Entities.Lancamentos;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <param name="Ate">Inclusiva. O chamador informa o último dia do mês, não o primeiro do seguinte.</param>
public sealed record FiltroDeLancamento(DateOnly De, DateOnly Ate, bool PorCaixa, Guid? CategoriaId);

public interface ILancamentoReadRepository
{
    Task<IReadOnlyList<LancamentoEntity>> ListarAsync(
        FiltroDeLancamento filtro,
        CancellationToken cancellationToken);

    Task<LancamentoEntity?> ObterAsync(Guid lancamentoId, CancellationToken cancellationToken);
}

/// <summary>Leitura das participações. Base de toda apuração de valor por pessoa.</summary>
public interface ILancamentoParticipacaoReadRepository
{
    Task<IReadOnlyList<LancamentoParticipacaoEntity>> ListarPorLancamentoAsync(
        Guid lancamentoId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Escrita de lançamento e as leituras que a decidem, todas pelo primário.
/// </summary>
/// <remarks>
/// A participação não tem repositório de escrita próprio de propósito: lançamento sem
/// participação não é apurável por nenhum relatório, então os dois nascem e morrem na mesma
/// transação. Um repositório separado abriria sua própria transação e permitiria exatamente
/// esse estado inválido.
/// </remarks>
public interface ILancamentoWriteRepository
{
    /// <summary>
    /// Grava o lançamento — ou o conjunto de parcelas — junto das participações.
    /// </summary>
    Task CriarAsync(
        IReadOnlyList<LancamentoEntity> lancamentos,
        IReadOnlyList<LancamentoParticipacaoEntity> participacoes,
        CancellationToken cancellationToken);

    /// <summary>Carga antes de alterar. Leitura no fluxo de decisão.</summary>
    Task<LancamentoEntity?> ObterAsync(Guid lancamentoId, CancellationToken cancellationToken);

    /// <summary>Possíveis duplicados: mesma descrição, mesmo valor e mesma competência.</summary>
    Task<int> ContarSemelhantesAsync(
        string descricao,
        decimal valor,
        DateOnly dataCompetencia,
        CancellationToken cancellationToken);

    Task<bool> AlterarAsync(LancamentoEntity lancamento, CancellationToken cancellationToken);

    Task<bool> RemoverAsync(Guid lancamentoId, CancellationToken cancellationToken);
}
