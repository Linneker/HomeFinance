using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.DefinirOrcamento;

/// <param name="Competencia">
/// Qualquer dia do mês orçado. É reduzido ao primeiro dia antes de gravar.
/// </param>
public sealed record DefinirOrcamentoCommand(
    Guid TenantId,
    Guid CategoriaId,
    DateOnly Competencia,
    decimal ValorPlanejado) : IRequest<DefinirOrcamentoCommandResult>;
