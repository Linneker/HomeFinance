using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Classificacao.Command.RemoverRegra;
using Acme.Sistemas.Services.V1.Classificacao.Query.Regras;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.RegraClassificacao;

/// <summary>
/// Gestão das regras do tenant ativo.
/// </summary>
/// <remarks>
/// Não há rota de criação: regra nasce da correção manual de um lançamento, na rota de
/// classificação. Criar regra no vazio exigiria adivinhar o padrão antes de ver uma descrição
/// real, que é justamente o que a fatura ruidosa torna difícil.
/// </remarks>
public sealed class RegraClassificacao : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/api/v1/regras-classificacao", async (
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new RegrasQuery(contexto.TenantId!.Value), cancellationToken);

                return Results.Ok(resultado.ParaResponse());
            })
            .RequireAuthorization()
            .WithName("ListarRegrasClassificacao")
            .WithTags("Classificação");

        rotas.MapPut("/api/v1/regras-classificacao/{regraId:guid}", async (
                Guid regraId,
                AlterarRegraRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    request.ParaCommand(contexto.TenantId!.Value, regraId), cancellationToken);

                return resultado.Sucesso
                    ? Results.Ok(resultado.ParaResponse())
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("AlterarRegraClassificacao")
            .WithTags("Classificação");

        rotas.MapDelete("/api/v1/regras-classificacao/{regraId:guid}", async (
                Guid regraId,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new RemoverRegraCommand(contexto.TenantId!.Value, regraId), cancellationToken);

                return resultado.Sucesso
                    ? Results.NoContent()
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("RemoverRegraClassificacao")
            .WithTags("Classificação");
    }
}
