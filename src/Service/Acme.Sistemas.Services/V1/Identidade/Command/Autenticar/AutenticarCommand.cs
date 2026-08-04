using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Identidade.Command.Autenticar;

public sealed record AutenticarCommand(string Email, string Senha) : IRequest<AutenticarCommandResult>;
