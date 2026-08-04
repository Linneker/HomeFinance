using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Identidade.Command.ExcluirConta;

/// <summary>
/// Registra a execução da exclusão.
/// </summary>
/// <remarks>
/// Depois do DELETE não sobra nenhum vestígio no banco — por construção, é isso que a exclusão
/// significa. O log da execução é a única prova de que o pedido do titular foi atendido, e por
/// isso registra o identificador: ele já não aponta para dado pessoal algum.
/// </remarks>
public sealed class ExcluirContaCommandBehavior(ILogger<ExcluirContaCommandBehavior> logger)
    : IPipelineBehavior<ExcluirContaCommand, ExcluirContaCommandResult>
{
    public async Task<ExcluirContaCommandResult> HandleAsync(
        ExcluirContaCommand request,
        RequestHandlerDelegate<ExcluirContaCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Conta {UsuarioId} excluída a pedido do titular.", request.UsuarioId);
        }

        return resultado;
    }
}
