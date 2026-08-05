using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Planejamento.Query.OrcamentoDoMes;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Orcamento;

/// <summary>
/// Orçamento mensal por categoria do tenant ativo.
/// </summary>
/// <remarks>
/// A competência é segmento de rota e não corpo: o mês identifica o recurso, e tê-lo na URL é o
/// que torna a tela do mês endereçável e o cache do cliente possível por mês.
/// </remarks>
public sealed class Orcamento : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/api/v1/orcamentos/{competencia:datetime}", async (
                DateTime competencia,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new OrcamentoDoMesQuery(
                        contexto.TenantId!.Value, DateOnly.FromDateTime(competencia)),
                    cancellationToken);

                return Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("ObterOrcamentoDoMes")
            .WithTags("Planejamento");

        rotas.MapPut("/api/v1/orcamentos/{competencia:datetime}", async (
                DateTime competencia,
                DefinirOrcamentoRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    request.ParaCommand(
                        contexto.TenantId!.Value, DateOnly.FromDateTime(competencia)),
                    cancellationToken);

                return resultado.Sucesso
                    ? Results.Ok(resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("DefinirOrcamento")
            .WithTags("Planejamento");

        rotas.MapPost("/api/v1/orcamentos/copias", async (
                CopiarOrcamentoRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    request.ParaCommand(contexto.TenantId!.Value), cancellationToken);

                return resultado.Sucesso
                    ? Results.Ok(resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("CopiarOrcamento")
            .WithTags("Planejamento");
    }
}
