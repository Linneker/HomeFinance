using Acme.Sistemas.Domain.Entities.Planejamento;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>Leitura do orçamento para a tela do mês.</summary>
public interface IOrcamentoReadRepository
{
    Task<IReadOnlyList<OrcamentoMensalEntity>> ListarDoMesAsync(
        DateOnly competencia,
        CancellationToken cancellationToken);
}

/// <summary>Escrita do orçamento e as leituras que a decidem, todas pelo primário.</summary>
public interface IOrcamentoWriteRepository
{
    Task<IReadOnlyList<OrcamentoMensalEntity>> ListarDoMesAsync(
        DateOnly competencia,
        CancellationToken cancellationToken);

    /// <summary>
    /// Grava o valor da categoria no mês, substituindo o que houver.
    /// </summary>
    /// <remarks>
    /// Substituição e não erro de duplicidade: definir de novo o orçamento de uma categoria é
    /// exatamente a operação de ajustar o plano, e é o que a tela faz a cada edição.
    /// </remarks>
    Task GravarAsync(OrcamentoMensalEntity orcamento, CancellationToken cancellationToken);

    /// <summary>
    /// Substitui o mês de destino inteiro pelas linhas informadas, numa transação só.
    /// </summary>
    /// <returns>Quantidade de linhas gravadas.</returns>
    /// <remarks>
    /// A cópia apaga antes de inserir porque copiar julho para agosto significa "agosto passa a
    /// ser igual a julho". Sem o apagamento, uma categoria que existia só em agosto sobreviveria
    /// à cópia e o mês ficaria com um plano que não é nem o de julho nem o que estava lá.
    /// </remarks>
    Task<int> SubstituirMesAsync(
        DateOnly competencia,
        IReadOnlyList<OrcamentoMensalEntity> orcamentos,
        CancellationToken cancellationToken);

    Task<bool> RemoverAsync(Guid orcamentoId, CancellationToken cancellationToken);
}
