using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Relatorios.Query.ExtratoDoMes;
using Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Relatorio;

/// <summary>
/// Relatórios do mês: extrato detalhado e resumo por categoria.
/// </summary>
/// <remarks>
/// O mês é segmento de rota porque identifica o recurso; a visão é query string porque é
/// recorte do mesmo recurso. Mês na rota é o que torna a tela endereçável — a pessoa consegue
/// guardar o link de agosto.
/// </remarks>
public sealed class Relatorio : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/api/v1/relatorios/{competencia:datetime}/extrato", async (
                DateTime competencia,
                [FromQuery] bool? porCaixa,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new ExtratoDoMesQuery(
                        contexto.TenantId!.Value,
                        DateOnly.FromDateTime(competencia),
                        porCaixa ?? false),
                    cancellationToken);

                return Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("ExtratoDoMes")
            .WithTags("Relatórios");

        rotas.MapGet("/api/v1/relatorios/{competencia:datetime}/resumo", async (
                DateTime competencia,
                [FromQuery] bool? porCaixa,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new ResumoDoMesQuery(
                        contexto.TenantId!.Value,
                        DateOnly.FromDateTime(competencia),
                        porCaixa ?? false),
                    cancellationToken);

                return Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("ResumoDoMes")
            .WithTags("Relatórios");
    }
}
