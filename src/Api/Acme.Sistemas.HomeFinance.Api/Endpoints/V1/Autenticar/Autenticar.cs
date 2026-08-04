using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Autenticar;

public sealed class Autenticar : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas) =>
        rotas.MapPost("/api/v1/sessoes", async (
                AutenticarRequest request,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(request.ParaCommand(), cancellationToken);

                // 401 e não 400: credencial inválida é falha de autenticação, e devolver o
                // mesmo status de "campo malformado" confundiria o cliente.
                return resultado.Sucesso
                    ? Results.Ok(resultado.ParaResponse())
                    : Results.Json(new { erros = resultado.Erros }, statusCode: StatusCodes.Status401Unauthorized);
            })
            .AllowAnonymous()
            .WithName("Autenticar")
            .WithTags("Identidade");
}
