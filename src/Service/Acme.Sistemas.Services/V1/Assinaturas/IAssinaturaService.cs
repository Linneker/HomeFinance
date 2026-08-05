using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Assinaturas;

/// <param name="CotaTotal">Compartilhamentos que o plano concede.</param>
/// <param name="CotaUsada">Compartilhamentos vigentes. Revogar devolve cota.</param>
public sealed record SituacaoDaConta(
    SituacaoAssinatura Situacao,
    string PlanoCodigo,
    int CotaTotal,
    int CotaUsada)
{
    public bool Vigente => Situacao is SituacaoAssinatura.Ativa or SituacaoAssinatura.Carencia;

    public int CotaDisponivel => Math.Max(0, CotaTotal - CotaUsada);

    /// <summary>Conta sem assinatura nenhuma. Trata-se como suspensa: nada de recurso derivado.</summary>
    public static readonly SituacaoDaConta SemAssinatura =
        new(SituacaoAssinatura.Suspensa, string.Empty, 0, 0);
}

/// <summary>
/// Fonte única da situação de assinatura para decisão de acesso.
/// </summary>
/// <remarks>
/// Única de propósito. Se cada ponto do sistema resolvesse por conta própria "esta conta pode?",
/// bastaria um deles esquecer a carência para a conta ficar suspensa num lugar e ativa em outro
/// — e o suporte não teria como reproduzir.
/// </remarks>
public interface IAssinaturaService
{
    Task<SituacaoDaConta> SituacaoAtualAsync(CancellationToken cancellationToken);

    /// <summary>
    /// A assinatura do dono do tenant informado está vigente?
    /// </summary>
    /// <remarks>
    /// Calculada, não armazenada. É o que faz a suspensão do dono derrubar os acessos
    /// compartilhados e a regularização devolvê-los sem reconceder nada: não há estado a virar
    /// nos dois sentidos, e portanto não há estado para ficar preso no meio do caminho.
    /// </remarks>
    Task<bool> DonoDoTenantVigenteAsync(Guid tenantId, CancellationToken cancellationToken);
}
