using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Compartilhamento.Query.ItensDoConsentimento;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.CompartilharTenant;

/// <summary>Concessão, revogação e a lista itemizada que precede a concessão.</summary>
public sealed class CompartilharTenant : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        // Antes de compartilhar, o dono precisa ver o que ficará visível. Este GET é o
        // conteúdo daquela tela — sem ele, "confirmei o consentimento" não significa nada.
        rotas.MapGet("/api/v1/compartilhamentos/consentimento", async (
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new ItensDoConsentimentoQuery(contexto.UsuarioId!.Value), cancellationToken);

                return Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("ItensDoConsentimento")
            .WithTags("Compartilhamento");

        rotas.MapPost("/api/v1/compartilhamentos", async (
                CompartilharTenantRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    request.ParaCommand(contexto.UsuarioId!.Value, contexto.TenantId), cancellationToken);

                return resultado.Sucesso
                    ? Results.Ok(resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("CompartilharTenant")
            .WithTags("Compartilhamento");

        // O destinatário na rota, não no corpo: DELETE com corpo tem suporte irregular em
        // proxies e clientes HTTP, e o roteamento de Minimal API não infere corpo em DELETE.
        rotas.MapDelete("/api/v1/compartilhamentos/{destinatarioId:guid}", async (
                Guid destinatarioId,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    CompartilharTenantMap.ParaCommandRevogacao(
                        contexto.UsuarioId!.Value, contexto.TenantId, destinatarioId),
                    cancellationToken);

                return resultado.Sucesso
                    ? Results.NoContent()
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("RevogarCompartilhamento")
            .WithTags("Compartilhamento");
    }
}
