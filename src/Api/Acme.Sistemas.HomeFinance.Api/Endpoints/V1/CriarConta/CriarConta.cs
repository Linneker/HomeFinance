using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.CriarConta;

public sealed class CriarConta : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas) =>
        rotas.MapPost("/api/v1/contas", async (
                CriarContaRequest request,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(request.ParaCommand(), cancellationToken);

                return resultado.Sucesso
                    ? Results.Created($"/api/v1/contas/{resultado.UsuarioId}", resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .AllowAnonymous()
            .WithName("CriarConta")
            .WithTags("Identidade");
}
