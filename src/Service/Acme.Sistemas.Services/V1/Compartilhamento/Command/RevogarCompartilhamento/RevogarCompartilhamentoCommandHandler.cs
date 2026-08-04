using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Command.RevogarCompartilhamento;

/// <summary>
/// Revoga o acesso concedido a alguém.
/// </summary>
/// <remarks>
/// O efeito é imediato porque a resolução de tenant roda a cada requisição e consulta o
/// primário: na requisição seguinte o destinatário já recebe 403, sem esperar token expirar.
/// </remarks>
public sealed class RevogarCompartilhamentoCommandHandler(
    ITenantWriteRepository tenants,
    TimeProvider relogio)
    : IRequestHandler<RevogarCompartilhamentoCommand, RevogarCompartilhamentoCommandResult>
{
    public const string CodigoTenantNaoEncontrado = "tenant_nao_encontrado";
    public const string CodigoCompartilhamentoInexistente = "compartilhamento_inexistente";
    public const string CodigoRevogacaoForaDoProprioTenant = "revogacao_fora_do_proprio_tenant";

    public async Task<RevogarCompartilhamentoCommandResult> HandleAsync(
        RevogarCompartilhamentoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = RevogarCompartilhamentoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return RevogarCompartilhamentoCommandResult.Recusado(erros[0]);
        }

        // Só o dono revoga: o tenant sai do usuário autenticado, nunca do pedido.
        var tenantId = await tenants.ObterTenantDoUsuarioAsync(request.DonoUsuarioId, cancellationToken);

        if (tenantId is null)
        {
            return RevogarCompartilhamentoCommandResult.Recusado(
                new Error(CodigoTenantNaoEncontrado, "Tenant do titular não encontrado."));
        }

        if (request.TenantSelecionadoId is not null && request.TenantSelecionadoId != tenantId)
        {
            return RevogarCompartilhamentoCommandResult.Recusado(
                new Error(
                    CodigoRevogacaoForaDoProprioTenant,
                    "Só é possível revogar acessos do seu próprio espaço."));
        }

        var revogou = await tenants.RevogarAsync(
            tenantId.Value, request.UsuarioDestinatarioId, relogio.GetUtcNow(), cancellationToken);

        return revogou
            ? RevogarCompartilhamentoCommandResult.Revogado()
            : RevogarCompartilhamentoCommandResult.Recusado(
                new Error(CodigoCompartilhamentoInexistente, "Não há acesso vigente para revogar."));
    }
}
