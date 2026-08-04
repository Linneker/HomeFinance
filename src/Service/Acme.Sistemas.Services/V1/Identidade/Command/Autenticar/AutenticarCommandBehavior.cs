using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Identidade.Command.Autenticar;

/// <summary>
/// Registro das tentativas de autenticação, sem e-mail e sem token.
/// </summary>
/// <remarks>
/// Log de autenticação é a matéria-prima da auditoria de acesso, mas colocar o e-mail ali
/// transforma o índice de logs numa cópia da base de clientes.
/// </remarks>
public sealed class AutenticarCommandBehavior(ILogger<AutenticarCommandBehavior> logger)
    : IPipelineBehavior<AutenticarCommand, AutenticarCommandResult>
{
    public async Task<AutenticarCommandResult> HandleAsync(
        AutenticarCommand request,
        RequestHandlerDelegate<AutenticarCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Autenticação concluída.");
        }
        else
        {
            logger.LogWarning("Tentativa de autenticação recusada.");
        }

        return resultado;
    }
}
