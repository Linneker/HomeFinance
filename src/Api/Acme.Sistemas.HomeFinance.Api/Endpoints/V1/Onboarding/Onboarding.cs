using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Onboarding.Query.ProgressoOnboarding;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Onboarding;

/// <summary>
/// Barra de progresso do onboarding do tenant ativo.
/// </summary>
/// <remarks>
/// Só leitura. Não há rota para marcar um passo como cumprido: cada requisito é cumprido pela
/// ação real que ele representa — declarar renda cumpre a renda declarada. Uma rota de marcação
/// permitiria destravar o produto sem fazer nada do que ele pede.
/// </remarks>
public sealed class Onboarding : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas) =>
        rotas.MapGet("/api/v1/onboarding", async (
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new ProgressoOnboardingQuery(contexto.TenantId!.Value), cancellationToken);

                return Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("ProgressoDoOnboarding")
            .WithTags("Onboarding");
}
