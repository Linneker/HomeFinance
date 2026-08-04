using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Lancamentos.Command.RemoverLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Query.Lancamentos;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Lancamento;

public sealed class Lancamento : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapPost("/api/v1/lancamentos", async (
                CriarLancamentoRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    request.ParaCommand(contexto.TenantId!.Value, contexto.UsuarioId!.Value),
                    cancellationToken);

                if (resultado.Sucesso)
                {
                    return Results.Created(
                        $"/api/v1/lancamentos/{resultado.LancamentoIds[0]}", resultado.ParaResponse());
                }

                // 409 e não 400: nada está errado no que foi enviado. O estado atual do tenant
                // é que exige uma decisão, e o cliente resolve reenviando com a confirmação.
                return resultado.AguardandoConfirmacaoDeDuplicidade
                    ? Results.Conflict(resultado.ParaAvisoDeDuplicidade())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("CriarLancamento")
            .WithTags("Lançamentos");

        rotas.MapGet("/api/v1/lancamentos", async (
                [FromQuery] DateOnly? de,
                [FromQuery] DateOnly? ate,
                // Anulável, e não `bool`: um `bool` obrigatório faz a Minimal API recusar a
                // requisição que o omite — e omiti-lo é o caso normal, porque competência é
                // a visão padrão do produto.
                [FromQuery] bool? porCaixa,
                [FromQuery] Guid? categoriaId,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new LancamentosQuery(
                        contexto.TenantId!.Value, de, ate, porCaixa ?? false, categoriaId),
                    cancellationToken);

                return resultado.Erros.Count > 0
                    ? RespostaHttp.Falha(resultado.Erros)
                    : Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("ListarLancamentos")
            .WithTags("Lançamentos");

        rotas.MapPut("/api/v1/lancamentos/{lancamentoId:guid}", async (
                Guid lancamentoId,
                AlterarLancamentoRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    request.ParaCommand(contexto.TenantId!.Value, lancamentoId), cancellationToken);

                return resultado.Sucesso
                    ? Results.Ok(resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("AlterarLancamento")
            .WithTags("Lançamentos");

        rotas.MapDelete("/api/v1/lancamentos/{lancamentoId:guid}", async (
                Guid lancamentoId,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new RemoverLancamentoCommand(contexto.TenantId!.Value, lancamentoId),
                    cancellationToken);

                return resultado.Sucesso
                    ? Results.NoContent()
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("RemoverLancamento")
            .WithTags("Lançamentos");
    }
}
