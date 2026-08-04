using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Compartilhamento.Query.TenantsDisponiveis;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.TenantsDisponiveis;

/// <summary>
/// Tenants que o usuário pode selecionar: o próprio e os recebidos por compartilhamento.
/// </summary>
/// <remarks>
/// Alimenta o seletor de tenant do app, então não depende do tenant já estar selecionado: o
/// resultado sai do usuário autenticado. Se dependesse, o app não teria como descobrir o que
/// pode abrir.
/// </remarks>
public sealed class TenantsDisponiveis : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas) =>
        rotas.MapGet("/api/v1/tenants", async (
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new TenantsDisponiveisQuery(contexto.UsuarioId!.Value), cancellationToken);

                return Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("TenantsDisponiveis")
            .WithTags("Compartilhamento");
}
