namespace Acme.Sistemas.HomeFinance.App.Services;

/// <summary>
/// Tenant ativo da sessão e se o acesso a ele é somente leitura.
/// </summary>
/// <remarks>
/// A escolha do tenant vai no cabeçalho <c>X-Tenant</c> de cada requisição. O modo somente
/// leitura é do compartilhamento recebido: quando ligado, a UI precisa indicá-lo de forma
/// permanente e inequívoca (a barra de contexto fica visível em todas as telas) e desabilitar as
/// ações de escrita — a recusa final é do banco, mas a UI não deve nem oferecer o que será negado.
/// </remarks>
public sealed class ContextoTenant
{
    /// <summary>Nome do cabeçalho de seleção de tenant, igual ao lido pelo middleware da Api.</summary>
    public const string CabecalhoTenant = "X-Tenant";

    public Guid? TenantId { get; private set; }

    public string? Rotulo { get; private set; }

    public bool SomenteLeitura { get; private set; }

    /// <summary>Disparado quando o tenant ativo muda, para as telas recarregarem e a barra atualizar.</summary>
    public event EventHandler? Alterado;

    public void Selecionar(Guid tenantId, string rotulo, bool somenteLeitura)
    {
        TenantId = tenantId;
        Rotulo = rotulo;
        SomenteLeitura = somenteLeitura;
        Alterado?.Invoke(this, EventArgs.Empty);
    }

    public void Limpar()
    {
        TenantId = null;
        Rotulo = null;
        SomenteLeitura = false;
        Alterado?.Invoke(this, EventArgs.Empty);
    }
}
