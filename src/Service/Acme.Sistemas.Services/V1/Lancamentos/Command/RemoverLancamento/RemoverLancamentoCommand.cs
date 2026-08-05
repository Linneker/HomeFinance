using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.RemoverLancamento;

public sealed record RemoverLancamentoCommand(
    Guid TenantId,
    Guid LancamentoId)
    : IRequest<RemoverLancamentoCommandResult>, IEscritaFinanceira;
