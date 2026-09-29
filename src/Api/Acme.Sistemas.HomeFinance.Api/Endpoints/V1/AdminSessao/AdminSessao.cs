using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.AdminSessao;

/// <summary>
/// Login do back-office. Emite um token de perfil administrativo, sem tenant.
/// </summary>
/// <remarks>
/// É o par do <c>/api/v1/sessoes</c> do usuário final, e os dois não se cruzam: cada um busca a
/// credencial na sua própria tabela, então uma senha de administrador não entra no login do
/// usuário e vice-versa. O token emitido aqui carrega a claim de perfil que as rotas
/// administrativas exigem — o token do usuário final não a tem.
/// </remarks>
public sealed class AdminSessao : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas) =>
        rotas.MapPost("/api/v1/admin/sessoes", async (
                AdminSessaoRequest request,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(request.ParaCommand(), cancellationToken);

                return resultado.Sucesso
                    ? Results.Ok(resultado.ParaResponse())
                    : Results.Json(new { erros = resultado.Erros }, statusCode: StatusCodes.Status401Unauthorized);
            })
            .AllowAnonymous()
            .WithName("AutenticarAdministrador")
            .WithTags("Administração");
}
