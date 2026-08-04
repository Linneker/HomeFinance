using Acme.Sistemas.Infrastructure.Databases.Configuration;

namespace Acme.Sistemas.HomeFinance.Api.Config;

/// <summary>
/// Contexto de isolamento da requisição, preenchido pelo <see cref="ResolucaoDeTenantMiddleware"/>.
/// </summary>
/// <remarks>
/// Os defaults são fechados de propósito: sem preenchimento não há usuário nem tenant, e
/// <see cref="SomenteLeitura"/> começa verdadeiro. Um caminho que esqueça de estabelecer
/// contexto resulta em consulta vazia e escrita recusada — nunca em acesso indevido.
/// <para>
/// É mutável e <c>Scoped</c> porque o tenant só pode ser resolvido depois da autenticação, mas
/// precisa estar disponível para todo o resto da requisição — inclusive para a própria consulta
/// que o resolve, que roda com usuário definido e tenant ainda nulo.
/// </para>
/// </remarks>
public sealed class ContextoRequisicaoHttp : IContextoRequisicao
{
    public Guid? UsuarioId { get; private set; }

    public Guid? TenantId { get; private set; }

    public bool SomenteLeitura { get; private set; } = true;

    public void DefinirUsuario(Guid usuarioId) => UsuarioId = usuarioId;

    public void DefinirTenant(Guid tenantId, bool somenteLeitura)
    {
        TenantId = tenantId;
        SomenteLeitura = somenteLeitura;
    }
}
