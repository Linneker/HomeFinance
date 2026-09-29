using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Services.V1.Administracao.Command.SalvarPlano;
using Acme.Sistemas.Services.V1.Administracao.Query.ListarPlanos;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.AdminPlanos;

/// <summary>
/// Manutenção do catálogo de planos e das cotas de compartilhamento — exclusiva da operação.
/// </summary>
public sealed class AdminPlanos : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/api/v1/admin/planos", async (
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(new ListarPlanosQuery(), cancellationToken);

                return Results.Ok(resultado.Planos);
            })
            .RequireAuthorization(PerfilAdministrativo.Politica)
            .WithName("ListarPlanosAdministrativos")
            .WithTags("Administração");

        rotas.MapPost("/api/v1/admin/planos", async (
                AdminPlanoRequest request,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new SalvarPlanoCommand(
                        request.Codigo,
                        request.Nome,
                        request.CotaCompartilhamentos,
                        request.PrecoMensal,
                        request.Ativo),
                    cancellationToken);

                if (!resultado.Sucesso)
                {
                    return RespostaHttp.Falha(resultado.Erros);
                }

                var corpo = new { planoId = resultado.PlanoId, criado = resultado.Criado };

                // 201 quando cria, 200 quando atualiza: o cliente distingue a inclusão da edição.
                return resultado.Criado
                    ? Results.Created($"/api/v1/admin/planos/{request.Codigo}", corpo)
                    : Results.Ok(corpo);
            })
            .RequireAuthorization(PerfilAdministrativo.Politica)
            .WithName("SalvarPlanoAdministrativo")
            .WithTags("Administração");
    }
}
