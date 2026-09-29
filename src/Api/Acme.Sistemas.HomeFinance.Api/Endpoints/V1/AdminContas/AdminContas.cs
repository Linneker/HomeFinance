using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Services.V1.Administracao.Query.ListarContas;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.AdminContas;

/// <summary>
/// Relação de contas para o back-office — exclusiva do perfil administrativo.
/// </summary>
/// <remarks>
/// Devolve identificação e situação de cada conta, sem qualquer valor financeiro. A requisição
/// não seleciona tenant, e não há função que dê ao administrador acesso ao dado do cliente:
/// mesmo com a exportação habilitada para uma conta, nenhuma linha de tabela financeira chega
/// até aqui.
/// </remarks>
public sealed class AdminContas : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas) =>
        rotas.MapGet("/api/v1/admin/contas", async (
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(new ListarContasQuery(), cancellationToken);

                return Results.Ok(resultado.Contas);
            })
            .RequireAuthorization(PerfilAdministrativo.Politica)
            .WithName("ListarContasAdministrativas")
            .WithTags("Administração");
}
