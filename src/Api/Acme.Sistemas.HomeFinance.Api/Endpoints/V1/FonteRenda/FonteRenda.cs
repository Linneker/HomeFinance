using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Planejamento.Command.RemoverFonteRenda;
using Acme.Sistemas.Services.V1.Planejamento.Query.FontesRenda;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.FonteRenda;

/// <summary>
/// Renda declarada do tenant ativo.
/// </summary>
public sealed class FonteRenda : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/api/v1/rendas", async (
                // Anulável: omitir devolve o histórico de vigências, que é a visão de quem
                // quer conferir como a renda mudou ao longo do tempo.
                [FromQuery] DateOnly? competencia,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new FontesRendaQuery(contexto.TenantId!.Value, competencia), cancellationToken);

                return Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("ListarFontesRenda")
            .WithTags("Planejamento");

        rotas.MapPost("/api/v1/rendas", async (
                CadastrarFonteRendaRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                TimeProvider relogio,
                CancellationToken cancellationToken) =>
            {
                var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);

                var resultado = await mediator.SendAsync(
                    request.ParaCommand(contexto.TenantId!.Value, hoje), cancellationToken);

                return resultado.Sucesso
                    ? Results.Created(
                        $"/api/v1/rendas/{resultado.FonteId}", resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("CadastrarFonteRenda")
            .WithTags("Planejamento");

        rotas.MapPut("/api/v1/rendas/{fonteId:guid}", async (
                Guid fonteId,
                AlterarFonteRendaRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                TimeProvider relogio,
                CancellationToken cancellationToken) =>
            {
                var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);

                var resultado = await mediator.SendAsync(
                    request.ParaCommand(contexto.TenantId!.Value, fonteId, hoje), cancellationToken);

                return resultado.Sucesso
                    ? Results.Ok(resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("AlterarFonteRenda")
            .WithTags("Planejamento");

        rotas.MapDelete("/api/v1/rendas/{fonteId:guid}", async (
                Guid fonteId,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new RemoverFonteRendaCommand(contexto.TenantId!.Value, fonteId),
                    cancellationToken);

                return resultado.Sucesso
                    ? Results.NoContent()
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("RemoverFonteRenda")
            .WithTags("Planejamento");
    }
}
