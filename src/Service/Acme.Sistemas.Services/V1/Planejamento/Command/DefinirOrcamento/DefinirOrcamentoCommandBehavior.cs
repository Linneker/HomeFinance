using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.DefinirOrcamento;

public sealed class DefinirOrcamentoCommandBehavior(
    ILogger<DefinirOrcamentoCommandBehavior> logger)
    : IPipelineBehavior<DefinirOrcamentoCommand, DefinirOrcamentoCommandResult>
{
    public async Task<DefinirOrcamentoCommandResult> HandleAsync(
        DefinirOrcamentoCommand request,
        RequestHandlerDelegate<DefinirOrcamentoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso && resultado.Aviso is not null)
        {
            // Só o excesso vai para log, e sem os valores: o que interessa operacionalmente é
            // com que frequência o produto avisa, não quanto cada pessoa planeja gastar.
            logger.LogInformation(
                "Orçamento de {Competencia} gravado acima da renda prevista.",
                resultado.Competencia);
        }

        return resultado;
    }
}
