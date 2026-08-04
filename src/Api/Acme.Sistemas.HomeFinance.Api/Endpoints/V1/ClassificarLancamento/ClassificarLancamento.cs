using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.ClassificarLancamento;

/// <summary>
/// Classificação manual de um lançamento, com a oferta de virar regra.
/// </summary>
/// <remarks>
/// Rota própria, separada da alteração do lançamento: classificar é a ação frequente da fila
/// de pendentes e não deve exigir reenviar descrição, valor e as duas datas só para escolher
/// uma categoria.
/// </remarks>
public sealed class ClassificarLancamento : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas) =>
        rotas.MapPut("/api/v1/lancamentos/{lancamentoId:guid}/classificacao", async (
                Guid lancamentoId,
                ClassificarLancamentoRequest request,
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
            .WithName("ClassificarLancamento")
            .WithTags("Classificação");
}
