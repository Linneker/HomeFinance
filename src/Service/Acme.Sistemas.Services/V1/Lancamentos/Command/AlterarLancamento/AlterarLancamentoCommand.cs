using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.AlterarLancamento;

/// <param name="CategoriaId">
/// Nula limpa a classificação e devolve o lançamento à fila de pendentes.
/// </param>
public sealed record AlterarLancamentoCommand(
    Guid TenantId,
    Guid LancamentoId,
    string Descricao,
    decimal Valor,
    DateOnly? DataCompetencia,
    DateOnly? DataCaixa,
    Guid? CategoriaId) : IRequest<AlterarLancamentoCommandResult>, IEscritaFinanceira;
