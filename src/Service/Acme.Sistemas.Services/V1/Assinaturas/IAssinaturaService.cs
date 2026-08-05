using Acme.Sistemas.Core.Response.Erros;
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

/// <param name="Erro">Nulo quando deu certo.</param>
public sealed record ResultadoDaContratacao(bool Sucesso, Guid? AssinaturaId, Error? Erro)
{
    public static ResultadoDaContratacao Contratada(Guid id) => new(true, id, null);

    public static ResultadoDaContratacao Recusada(string codigo, string mensagem) =>
        new(false, null, new Error(codigo, mensagem));
}

/// <param name="Processada">Falso quando a notificação já tinha sido recebida antes.</param>
public sealed record ResultadoDaNotificacao(bool Processada, SituacaoAssinatura? SituacaoResultante)
{
    public static readonly ResultadoDaNotificacao Repetida = new(false, null);
}

/// <summary>O que o provedor informou na notificação, já normalizado.</summary>
public enum EventoDoProvedor
{
    /// <summary>Renovação confirmada: estende o período pago.</summary>
    Renovacao = 1,

    /// <summary>O usuário cancelou na loja. Vale até o fim do período já pago.</summary>
    CancelamentoAgendado = 2,

    /// <summary>Reembolso ou estorno. Suspende na hora.</summary>
    Estorno = 3,
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

    /// <summary>
    /// Contrata um plano depois de confirmar o comprovante junto ao provedor.
    /// </summary>
    /// <remarks>
    /// Nenhum direito é concedido a partir do que o aplicativo afirmou. Um cliente é código que
    /// roda no aparelho de outra pessoa: acreditar nele significa que alterar um JSON destrava a
    /// assinatura de graça.
    /// </remarks>
    Task<ResultadoDaContratacao> ContratarAsync(
        string planoCodigo,
        CanalPagamento canal,
        string comprovante,
        CancellationToken cancellationToken);

    /// <summary>
    /// Aplica uma notificação do provedor, uma única vez.
    /// </summary>
    /// <remarks>
    /// A idempotência é do identificador do provedor, não da carga: o provedor reenvia até
    /// receber confirmação, e processar um estorno duas vezes suspenderia uma assinatura que já
    /// tinha sido regularizada no meio tempo.
    /// </remarks>
    Task<ResultadoDaNotificacao> AplicarNotificacaoAsync(
        CanalPagamento canal,
        string identificador,
        EventoDoProvedor evento,
        string referencia,
        string corpo,
        CancellationToken cancellationToken);
}
