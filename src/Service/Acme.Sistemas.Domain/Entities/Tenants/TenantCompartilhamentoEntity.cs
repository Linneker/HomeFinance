namespace Acme.Sistemas.Domain.Entities.Tenants;

/// <summary>
/// Concessão de acesso somente leitura ao tenant de outra pessoa.
/// </summary>
/// <remarks>
/// Assimétrica por natureza: Ana compartilhar com Bruno não dá a Ana acesso ao tenant do Bruno.
/// A revogação é marcação, não exclusão — o histórico de quem teve acesso e quando é o que
/// permite responder a uma contestação depois.
/// </remarks>
public sealed class TenantCompartilhamentoEntity : BaseEntity
{
    public Guid TenantId { get; set; }

    public Guid UsuarioDestinatarioId { get; set; }

    /// <summary>
    /// Dados do SCR ficam fora do compartilhamento por padrão: inadimplência e prejuízo são a
    /// informação mais constrangedora do sistema, e incluí-los exige ação deliberada do dono.
    /// </summary>
    public bool IncluiScr { get; set; }

    public DateTimeOffset ConcedidoEm { get; set; }

    public DateTimeOffset? RevogadoEm { get; set; }

    public bool Vigente => RevogadoEm is null;
}

/// <summary>Tenant que o usuário pode selecionar, com o modo de acesso resolvido.</summary>
public sealed record TenantDisponivel(Guid TenantId, bool EhProprio, bool SomenteLeitura);
