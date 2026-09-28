using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.HabilitarExportacao;

public sealed class HabilitarExportacaoCommandBehavior(
    ILogger<HabilitarExportacaoCommandBehavior> logger)
    : IPipelineBehavior<HabilitarExportacaoCommand, HabilitarExportacaoCommandResult>
{
    public async Task<HabilitarExportacaoCommandResult> HandleAsync(
        HabilitarExportacaoCommand request,
        RequestHandlerDelegate<HabilitarExportacaoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation(
                "Exportação habilitada para a conta {Conta} pelo administrador {Autor}.",
                request.ContaUsuarioId, request.AutorAdminId);
        }

        return resultado;
    }
}
