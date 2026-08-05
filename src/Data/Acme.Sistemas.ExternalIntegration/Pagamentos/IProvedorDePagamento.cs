using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.ExternalIntegration.Pagamentos;

/// <param name="Confirmado">O provedor reconhece a compra e ela está paga.</param>
/// <param name="Referencia">
/// Identificador da assinatura no provedor. É o que amarra a compra à conta do aplicativo e o
/// que impede o mesmo comprovante de ativar duas contas.
/// </param>
/// <param name="PeriodoFim">Até quando o período pago vale, segundo o provedor.</param>
/// <param name="Motivo">Por que não foi confirmado. Nulo quando foi.</param>
public sealed record ConfirmacaoDeCompra(
    bool Confirmado,
    string? Referencia,
    DateTimeOffset? PeriodoFim,
    string? Motivo)
{
    public static ConfirmacaoDeCompra Aceita(string referencia, DateTimeOffset periodoFim) =>
        new(true, referencia, periodoFim, null);

    public static ConfirmacaoDeCompra Recusada(string motivo) => new(false, null, null, motivo);
}

/// <summary>
/// Fala com um provedor de pagamento.
/// </summary>
/// <remarks>
/// Toda concessão de direito passa por aqui e nunca pelo que o aplicativo cliente afirmou. Um
/// cliente é código que roda no aparelho de outra pessoa: acreditar nele significa que alterar
/// um JSON destrava a assinatura de graça.
/// <para>
/// A interface é a mesma para loja e para gateway porque a decisão do sistema é a mesma —
/// "isto está pago e até quando?". O que muda é o protocolo, e isso fica em cada adaptador.
/// </para>
/// </remarks>
public interface IProvedorDePagamento
{
    CanalPagamento Canal { get; }

    /// <summary>Confirma um comprovante apresentado pelo aplicativo.</summary>
    Task<ConfirmacaoDeCompra> ConfirmarAsync(string comprovante, CancellationToken cancellationToken);

    /// <summary>
    /// Consulta a situação atual de uma assinatura já conhecida.
    /// </summary>
    /// <remarks>
    /// É o que a reconciliação usa. Existe porque notificação se perde: sem consulta ativa, uma
    /// renovação que não chegou deixaria a conta suspensa por tempo indeterminado, e um
    /// cancelamento que não chegou a deixaria ativa de graça.
    /// </remarks>
    Task<ConfirmacaoDeCompra> ConsultarAsync(string referencia, CancellationToken cancellationToken);
}
