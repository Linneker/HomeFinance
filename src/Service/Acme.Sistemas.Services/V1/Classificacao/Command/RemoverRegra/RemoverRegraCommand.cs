using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.RemoverRegra;

public sealed record RemoverRegraCommand(
    Guid TenantId,
    Guid RegraId) : IRequest<RemoverRegraCommandResult>;
