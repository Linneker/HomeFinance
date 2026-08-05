using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Domain.Entities.Assinaturas;

/// <summary>
/// Um evento de pagamento confirmado pelo provedor.
/// </summary>
/// <remarks>
/// Registro histórico: serve para explicar por que uma assinatura está no estado em que está.
/// Sem ele, uma suspensão por estorno seria indistinguível de uma suspensão por falta de
/// renovação, e o suporte não teria o que responder.
/// </remarks>
public sealed class TransacaoPagamentoEntity : BaseEntity
{
    public Guid AssinaturaId { get; set; }

    /// <summary>
    /// Dono da transação. Redundante com a assinatura de propósito: é o que a policy de RLS lê,
    /// e uma policy que precisasse do JOIN se tornaria recursiva com a de assinatura.
    /// </summary>
    public Guid UsuarioId { get; set; }

    public CanalPagamento Canal { get; set; }

    /// <summary>Identificador da transação no provedor. É a chave de idempotência.</summary>
    public required string IdentificadorProvedor { get; set; }

    public decimal Valor { get; set; }

    /// <summary>O que o provedor informou: confirmada, estornada, recusada.</summary>
    public required string Situacao { get; set; }

    public const string SituacaoConfirmada = "confirmada";

    public const string SituacaoEstornada = "estornada";

    public const string SituacaoRecusada = "recusada";
}
