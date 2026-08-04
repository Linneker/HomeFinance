using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Lancamentos.Command.EncerrarRecorrencia;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Recorrencia;

public sealed class Recorrencia : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapPost("/api/v1/recorrencias", async (
                CriarRecorrenciaRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    request.ParaCommand(contexto.TenantId!.Value, contexto.UsuarioId!.Value),
                    cancellationToken);

                return resultado.Sucesso
                    ? Results.Created($"/api/v1/recorrencias/{resultado.RecorrenciaId}", resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("CriarRecorrencia")
            .WithTags("Lançamentos");

        // DELETE encerra a regra; não apaga o que já aconteceu. As ocorrências previstas
        // futuras somem, as realizadas permanecem.
        rotas.MapDelete("/api/v1/recorrencias/{recorrenciaId:guid}", async (
                Guid recorrenciaId,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new EncerrarRecorrenciaCommand(contexto.TenantId!.Value, recorrenciaId),
                    cancellationToken);

                return resultado.Sucesso
                    ? Results.Ok(resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("EncerrarRecorrencia")
            .WithTags("Lançamentos");
    }
}
