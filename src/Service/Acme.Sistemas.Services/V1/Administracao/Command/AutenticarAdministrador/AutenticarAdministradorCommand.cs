using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Administracao.Command.AutenticarAdministrador;

/// <summary>Login do back-office. Identidade à parte da do usuário final.</summary>
public sealed record AutenticarAdministradorCommand(string Email, string Senha)
    : IRequest<AutenticarAdministradorCommandResult>;
