using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Alertas.Command.DispensarAlerta;

public sealed record DispensarAlertaCommand(Guid TenantId, Guid AlertaId)
    : IRequest<DispensarAlertaCommandResult>;
