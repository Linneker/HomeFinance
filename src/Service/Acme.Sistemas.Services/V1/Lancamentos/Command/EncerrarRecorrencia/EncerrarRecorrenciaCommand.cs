using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.EncerrarRecorrencia;

public sealed record EncerrarRecorrenciaCommand(
    Guid TenantId,
    Guid RecorrenciaId) : IRequest<EncerrarRecorrenciaCommandResult>, IEscritaFinanceira;
