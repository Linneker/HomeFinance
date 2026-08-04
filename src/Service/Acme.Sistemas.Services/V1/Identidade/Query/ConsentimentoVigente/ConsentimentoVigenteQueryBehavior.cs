using Acme.Sistemas.Core.Mediators;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Identidade.Query.ConsentimentoVigente;

public sealed class ConsentimentoVigenteQueryBehavior(
    ILogger<ConsentimentoVigenteQueryBehavior> logger)
    : IPipelineBehavior<ConsentimentoVigenteQuery, ConsentimentoVigenteQueryResult>
{
    public async Task<ConsentimentoVigenteQueryResult> HandleAsync(
        ConsentimentoVigenteQuery request,
        RequestHandlerDelegate<ConsentimentoVigenteQueryResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (!resultado.Vigente && resultado.Pendentes.Count > 0)
        {
            logger.LogInformation(
                "Aceite pendente para: {Documentos}.", string.Join(", ", resultado.Pendentes));
        }

        return resultado;
    }
}
