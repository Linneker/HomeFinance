using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Exportacao.Command.DesabilitarExportacao;
using Acme.Sistemas.Services.V1.Exportacao.Command.HabilitarExportacao;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.ExportacaoAdmin;

/// <summary>
/// Habilitação da exportação por conta — exclusiva do perfil administrativo.
/// </summary>
/// <remarks>
/// A política <c>administrador</c> é o que separa esta operação do usuário final: o token de
/// usuário não carrega a claim de perfil e é recusado na autorização, antes de chegar ao handler.
/// A habilitação não dá ao administrador nenhum acesso ao dado financeiro da conta — a gravação
/// não seleciona tenant.
/// </remarks>
public sealed class ExportacaoAdmin : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        rotas.MapPost("/api/v1/admin/contas/{contaUsuarioId:guid}/exportacao/habilitacao", async (
                Guid contaUsuarioId,
                ExportacaoAdminRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new HabilitarExportacaoCommand(contaUsuarioId, request.Motivo, contexto.UsuarioId ?? Guid.Empty),
                    cancellationToken);

                return resultado.Sucesso ? Results.NoContent() : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization(PerfilAdministrativo.Politica)
            .WithName("HabilitarExportacao")
            .WithTags("Exportação (admin)");

        rotas.MapPost("/api/v1/admin/contas/{contaUsuarioId:guid}/exportacao/desabilitacao", async (
                Guid contaUsuarioId,
                ExportacaoAdminRequest request,
                IMediator mediator,
                IContextoRequisicao contexto,
                CancellationToken cancellationToken) =>
            {
                var resultado = await mediator.SendAsync(
                    new DesabilitarExportacaoCommand(contaUsuarioId, request.Motivo, contexto.UsuarioId ?? Guid.Empty),
                    cancellationToken);

                return resultado.Sucesso ? Results.NoContent() : RespostaHttp.Falha(resultado.Erros);
            })
            .RequireAuthorization(PerfilAdministrativo.Politica)
            .WithName("DesabilitarExportacao")
            .WithTags("Exportação (admin)");
    }
}
