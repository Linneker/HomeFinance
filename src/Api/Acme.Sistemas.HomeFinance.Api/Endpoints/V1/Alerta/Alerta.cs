using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Alertas.Command.DispensarAlerta;
using Acme.Sistemas.Services.V1.Alertas.Query.AlertasAbertos;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Alerta;

/// <summary>
/// Alertas do tenant ativo.
/// </summary>
/// <remarks>
/// Não há rota de criação: alerta nasce da avaliação automática de uma condição, disparada ao
/// mexer em lançamento ou orçamento e pela rotina diária de caixa. Poder criar alerta pela API
/// permitiria fabricar avisos que não correspondem a nada no dado.
/// </remarks>
public sealed class Alerta : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/api/v1/alertas", async (
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new AlertasAbertosQuery(contexto.TenantId!.Value, contexto.SomenteLeitura),
                    cancellationToken);

                return Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("ListarAlertas")
            .WithTags("Alertas");

        rotas.MapPost("/api/v1/alertas/{alertaId:guid}/dispensa", async (
                Guid alertaId,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new DispensarAlertaCommand(contexto.TenantId!.Value, alertaId),
                    cancellationToken);

                return resultado.Sucesso
                    ? Results.NoContent()
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("DispensarAlerta")
            .WithTags("Alertas");
    }
}
