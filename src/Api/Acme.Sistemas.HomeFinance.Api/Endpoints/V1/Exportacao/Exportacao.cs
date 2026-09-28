using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Exportacao.Command.SolicitarExportacao;
using Acme.Sistemas.Services.V1.Exportacao.Query.BaixarExportacao;
using Acme.Sistemas.Services.V1.Exportacao.Query.SituacaoExportacao;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Exportacao;

/// <summary>
/// Exportação dos dados do próprio tenant.
/// </summary>
/// <remarks>
/// Nenhuma rota habilita o recurso — isso é do portal administrativo. Aqui o titular só solicita,
/// acompanha e baixa. A solicitação é recusada quando o recurso não está habilitado para a conta,
/// quando o tenant ativo é de terceiro, ou quando o formato não é oferecido.
/// </remarks>
public sealed class Exportacao : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapPost("/api/v1/exportacoes", async (
                SolicitarExportacaoRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new SolicitarExportacaoCommand(
                        contexto.TenantId!.Value,
                        contexto.UsuarioId!.Value,
                        contexto.SomenteLeitura,
                        request.Formato),
                    cancellationToken);

                return resultado.Sucesso
                    ? Results.AcceptedAtRoute("SituacaoExportacao", new { id = resultado.SolicitacaoId }, new SolicitarExportacaoResponse(resultado.SolicitacaoId!.Value))
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("SolicitarExportacao")
            .WithTags("Exportação");

        rotas.MapGet("/api/v1/exportacoes/{id:guid}", async (
                Guid id,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new SituacaoExportacaoQuery(contexto.TenantId!.Value, id), cancellationToken);

                return resultado.Encontrada
                    ? Results.Ok(resultado.ParaResponse())
                    : Results.NotFound();
            })
            .RequireAuthorization()
            .WithName("SituacaoExportacao")
            .WithTags("Exportação");

        rotas.MapGet("/api/v1/exportacoes/{id:guid}/arquivo", async (
                Guid id,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new BaixarExportacaoQuery(contexto.TenantId!.Value, contexto.UsuarioId!.Value, id),
                    cancellationToken);

                return resultado.Sucesso
                    ? Results.File(resultado.Conteudo!, resultado.ContentType!, resultado.NomeArquivo)
                    : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization()
            .WithName("BaixarExportacao")
            .WithTags("Exportação");
    }
}
