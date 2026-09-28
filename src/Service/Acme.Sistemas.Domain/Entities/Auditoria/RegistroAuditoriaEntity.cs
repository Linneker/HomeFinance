using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Domain.Entities.Auditoria;

/// <summary>
/// Uma entrada da trilha de auditoria.
/// </summary>
/// <remarks>
/// Append-only: a trilha só cresce. Uma auditoria que se pode editar não é auditoria — o valor
/// dela está exatamente em não poder ser reescrita depois do fato.
/// <para>
/// <see cref="ContaUsuarioId"/> é a conta sobre a qual o evento incide; <see cref="AutorId"/> é
/// quem o provocou. Nos eventos de habilitação os dois diferem — o administrador age sobre a
/// conta do titular; na solicitação e no download coincidem.
/// </para>
/// </remarks>
public sealed class RegistroAuditoriaEntity : BaseEntity
{
    public EventoAuditoria Evento { get; set; }

    public Guid ContaUsuarioId { get; set; }

    public Guid AutorId { get; set; }

    /// <summary>Detalhe legível do evento — formato pedido, motivo, referência do arquivo.</summary>
    public string? Detalhe { get; set; }
}
