using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarCategoria;

/// <summary>Registro da criação. Categoria é a chave do orçamento e dos relatórios.</summary>
public sealed class CriarCategoriaCommandBehavior(ILogger<CriarCategoriaCommandBehavior> logger)
    : IPipelineBehavior<CriarCategoriaCommand, CriarCategoriaCommandResult>
{
    public async Task<CriarCategoriaCommandResult> HandleAsync(
        CriarCategoriaCommand request,
        RequestHandlerDelegate<CriarCategoriaCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            logger.LogInformation("Categoria criada no tenant ativo.");
        }

        return resultado;
    }
}
