using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.ClassificarLancamento;

/// <param name="CriarRegra">
/// Aceite da oferta de transformar a correção em regra. Recusado, apenas aquele lançamento é
/// classificado e os futuros equivalentes continuam caindo em pendente.
/// </param>
public sealed record ClassificarLancamentoCommand(
    Guid TenantId,
    Guid LancamentoId,
    Guid CategoriaId,
    bool CriarRegra = false) : IRequest<ClassificarLancamentoCommandResult>;
