using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.RemoverFonteRenda;

public sealed record RemoverFonteRendaCommand(Guid TenantId, Guid FonteId)
    : IRequest<RemoverFonteRendaCommandResult>;
