using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.CopiarOrcamento;

public sealed class CopiarOrcamentoCommandBehavior(
    ILogger<CopiarOrcamentoCommandBehavior> logger)
    : IPipelineBehavior<CopiarOrcamentoCommand, CopiarOrcamentoCommandResult>
{
    public async Task<CopiarOrcamentoCommandResult> HandleAsync(
        CopiarOrcamentoCommand request,
        RequestHandlerDelegate<CopiarOrcamentoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation(
                "Orçamento copiado para {Destino}: {Linhas} categorias.",
                resultado.Destino,
                resultado.LinhasCopiadas);
        }

        return resultado;
    }
}
