using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Administracao.Command.SalvarPlano;

public sealed class SalvarPlanoCommandBehavior(ILogger<SalvarPlanoCommandBehavior> logger)
    : IPipelineBehavior<SalvarPlanoCommand, SalvarPlanoCommandResult>
{
    public async Task<SalvarPlanoCommandResult> HandleAsync(
        SalvarPlanoCommand request,
        RequestHandlerDelegate<SalvarPlanoCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation(
                "Plano {Codigo} {Acao} pela operação.",
                request.Codigo, resultado.Criado ? "criado" : "atualizado");
        }

        return resultado;
    }
}
