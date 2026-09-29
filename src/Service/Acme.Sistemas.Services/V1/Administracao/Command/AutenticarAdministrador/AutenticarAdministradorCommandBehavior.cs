using Acme.Sistemas.Core.Mediators;

namespace Acme.Sistemas.Services.V1.Administracao.Command.AutenticarAdministrador;

public sealed class AutenticarAdministradorCommandBehavior
    : IPipelineBehavior<AutenticarAdministradorCommand, AutenticarAdministradorCommandResult>
{
    public Task<AutenticarAdministradorCommandResult> HandleAsync(
        AutenticarAdministradorCommand request,
        RequestHandlerDelegate<AutenticarAdministradorCommandResult> next,
        CancellationToken cancellationToken) => next();
}
