using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Lancamentos.Query.Categorias;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Categoria;

public sealed class Categoria : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapPost("/api/v1/categorias", async (
                CategoriaRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                // O tenant vem do contexto resolvido pelo middleware, nunca do corpo.
                var resultado = await mediator.SendAsync(
                    request.ParaCommand(contexto.TenantId!.Value), cancellationToken);

                return resultado.Sucesso
                    ? Results.Created($"/api/v1/categorias/{resultado.CategoriaId}", resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("CriarCategoria")
            .WithTags("Lançamentos");

        rotas.MapGet("/api/v1/categorias", async (
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new CategoriasQuery(contexto.TenantId!.Value), cancellationToken);

                return Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("ListarCategorias")
            .WithTags("Lançamentos");
    }
}
