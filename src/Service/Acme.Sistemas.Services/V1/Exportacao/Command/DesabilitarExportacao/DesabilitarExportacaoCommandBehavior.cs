using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.DesabilitarExportacao;

public sealed class DesabilitarExportacaoCommandBehavior(
    ILogger<DesabilitarExportacaoCommandBehavior> logger)
    : IPipelineBehavior<DesabilitarExportacaoCommand, DesabilitarExportacaoCommandResult>
{
    public async Task<DesabilitarExportacaoCommandResult> HandleAsync(
        DesabilitarExportacaoCommand request,
        RequestHandlerDelegate<DesabilitarExportacaoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation(
                "Exportação desabilitada para a conta {Conta}; {Canceladas} pendente(s) cancelada(s).",
                request.ContaUsuarioId, resultado.PendentesCanceladas);
        }

        return resultado;
    }
}
