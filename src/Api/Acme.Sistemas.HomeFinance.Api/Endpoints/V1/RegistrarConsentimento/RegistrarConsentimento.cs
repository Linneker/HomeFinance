using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Identidade.Query.ConsentimentoVigente;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.RegistrarConsentimento;

public sealed class RegistrarConsentimento : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapPost("/api/v1/consentimentos", async (
                RegistrarConsentimentoRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                // O titular vem do token, nunca do corpo: aceitar usuarioId do cliente
                // permitiria registrar consentimento em nome de outra pessoa.
                var resultado = await mediator.SendAsync(
                    request.ParaCommand(contexto.UsuarioId!.Value), cancellationToken);

                return resultado.Sucesso ? Results.NoContent() : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("RegistrarConsentimento")
            .WithTags("Identidade");

        rotas.MapGet("/api/v1/consentimentos/vigencia", async (
                [FromQuery] string versaoAtual,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new ConsentimentoVigenteQuery(contexto.UsuarioId!.Value, versaoAtual), cancellationToken);

                return resultado.Erros.Count > 0
                    ? RespostaHttp.Falha(resultado.Erros)
                    : Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("ConsentimentoVigente")
            .WithTags("Identidade");
    }
}
