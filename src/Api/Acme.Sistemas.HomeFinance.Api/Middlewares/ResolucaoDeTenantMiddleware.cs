using System.Security.Claims;
using Acme.Sistemas.Core.Security;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Services.V1.Assinaturas;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Acme.Sistemas.HomeFinance.Api.Middlewares;

/// <summary>
/// Estabelece o contexto de isolamento da requisição a partir do token e do tenant selecionado.
/// </summary>
/// <remarks>
/// A policy de RLS confia no valor de <c>app.tenant_id</c>. Quem garante que o usuário pode
/// selecionar aquele tenant é esta validação — feita uma vez por requisição, contra o primário,
/// antes de qualquer consulta de dado financeiro.
/// <para>
/// A resolução consulta <c>tenant</c> e <c>tenant_compartilhamento</c>, cujas policies usam
/// <c>app.usuario_id</c> e não <c>app.tenant_id</c>. É por isso que essas tabelas foram isoladas
/// pelo usuário e não pelo tenant: sem isso, resolver o tenant exigiria o tenant já resolvido.
/// </para>
/// </remarks>
public sealed class ResolucaoDeTenantMiddleware(RequestDelegate proximo)
{
    /// <summary>Cabeçalho de seleção de tenant. Ausente, vale o tenant padrão do token.</summary>
    public const string CabecalhoTenant = "X-Tenant";

    /// <summary>Motivo devolvido quando a assinatura barra a abertura do tenant.</summary>
    public const string CabecalhoMotivo = "X-Acesso-Negado";

    public async Task InvokeAsync(
        HttpContext contexto,
        ContextoRequisicaoHttp contextoRequisicao,
        ITenantWriteRepository tenants,
        IAssinaturaService assinaturas)
    {
        if (contexto.User.Identity?.IsAuthenticated != true)
        {
            await proximo(contexto);
            return;
        }

        var usuarioId = LerGuid(contexto, ClaimTypes.NameIdentifier)
                        ?? LerGuid(contexto, JwtRegisteredClaimNames.Sub);

        if (usuarioId is null)
        {
            await proximo(contexto);
            return;
        }

        contextoRequisicao.DefinirUsuario(usuarioId.Value);

        var tenantSolicitado = LerTenantSolicitado(contexto);

        if (tenantSolicitado is null)
        {
            // Autenticado sem tenant: só operações de conta funcionam. Dado financeiro fica
            // invisível pela própria policy, sem precisar bloquear rota a rota.
            await proximo(contexto);
            return;
        }

        var acesso = await tenants.ResolverAcessoAsync(
            usuarioId.Value, tenantSolicitado.Value, contexto.RequestAborted);

        if (acesso is null)
        {
            // 403 e não 404: o usuário está autenticado, mas não tem acesso àquele tenant.
            // Um compartilhamento revogado cai aqui na requisição seguinte.
            contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Tenant próprio abre sempre, mesmo com assinatura suspensa: registrar e ler os
        // próprios lançamentos é o que a suspensão preserva, e excluir a conta também.
        if (!acesso.EhProprio && !await PodeAbrirCompartilhadoAsync(
                contexto, assinaturas, acesso.TenantId))
        {
            return;
        }

        contextoRequisicao.DefinirTenant(acesso.TenantId, acesso.SomenteLeitura);

        await proximo(contexto);
    }

    /// <summary>
    /// Compartilhamento é recurso derivado, e depende das duas assinaturas.
    /// </summary>
    /// <remarks>
    /// Nada é gravado nos dois sentidos: a suspensão do dono derruba o acesso porque a resposta
    /// desta pergunta muda, e a regularização o devolve pelo mesmo motivo — sem reconceder nada
    /// e sem estado para ficar preso no meio do caminho.
    /// <para>
    /// Os dois motivos são distintos na resposta porque a ação é distinta: quem precisa pagar é
    /// o convidado num caso e o dono no outro, e uma mensagem genérica mandaria a pessoa errada
    /// procurar o cartão.
    /// </para>
    /// </remarks>
    private static async Task<bool> PodeAbrirCompartilhadoAsync(
        HttpContext contexto,
        IAssinaturaService assinaturas,
        Guid tenantId)
    {
        var propria = await assinaturas.SituacaoAtualAsync(contexto.RequestAborted);

        if (!propria.Vigente)
        {
            Negar(contexto, "assinatura_propria_inativa");
            return false;
        }

        if (!await assinaturas.DonoDoTenantVigenteAsync(tenantId, contexto.RequestAborted))
        {
            Negar(contexto, "assinatura_do_dono_inativa");
            return false;
        }

        return true;
    }

    private static void Negar(HttpContext contexto, string motivo)
    {
        contexto.Response.Headers[CabecalhoMotivo] = motivo;
        contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
    }

    private static Guid? LerTenantSolicitado(HttpContext contexto)
    {
        if (contexto.Request.Headers.TryGetValue(CabecalhoTenant, out var valores)
            && Guid.TryParse(valores.ToString(), out var selecionado))
        {
            return selecionado;
        }

        return LerGuid(contexto, GeradorDeToken.ClaimTenantPadrao);
    }

    private static Guid? LerGuid(HttpContext contexto, string tipoClaim)
    {
        var valor = contexto.User.FindFirst(tipoClaim)?.Value;
        return Guid.TryParse(valor, out var identificador) ? identificador : null;
    }
}
