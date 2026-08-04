using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.ExcluirConta;

public sealed class ExcluirConta : IEndpoint
{
    // A confirmação vai na query, não no corpo: DELETE com corpo não é suportado pelo
    // roteamento de Minimal API e tem suporte irregular em proxies e clientes HTTP.
    public void Mapear(IEndpointRouteBuilder rotas) =>
        rotas.MapDelete("/api/v1/contas/minha", async (
                [FromQuery] bool confirmacao,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                // Só o próprio titular exclui a própria conta: o identificador vem do token.
                var resultado = await mediator.SendAsync(
                    ExcluirContaMap.ParaCommand(contexto.UsuarioId!.Value, confirmacao), cancellationToken);

                return resultado.Sucesso
                    ? Results.Ok(resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("ExcluirConta")
            .WithTags("Identidade");
}
