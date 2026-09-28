namespace Acme.Sistemas.Domain.Entities.Exportacao;

/// <summary>
/// Habilitação do recurso de exportação para uma conta.
/// </summary>
/// <remarks>
/// Presa ao usuário, não ao tenant: exportar é um direito do titular sobre a própria conta, e a
/// habilitação é concedida pela operação a pedido dele. Sem <c>tenant_id</c>, de propósito — ela
/// é lida antes de qualquer seleção de tenant, e mantê-la fora do escopo de tenant é o que impede
/// que ligar a exportação abra qualquer porta para o dado financeiro em si.
/// <para>
/// Uma linha por conta, atualizada no lugar: <see cref="Habilitada"/> alterna entre concessão e
/// revogação, e <see cref="Motivo"/>/<see cref="AutorId"/> guardam a última decisão. O histórico
/// completo de quem ligou e desligou fica na trilha de auditoria, não aqui.
/// </para>
/// </remarks>
public sealed class ExportacaoHabilitacaoEntity : BaseEntity
{
    public Guid UsuarioId { get; set; }

    public bool Habilitada { get; set; }

    /// <summary>Motivo informado pela operação. Obrigatório: sem motivo não se habilita.</summary>
    public required string Motivo { get; set; }

    /// <summary>Administrador que aplicou a última decisão.</summary>
    public Guid AutorId { get; set; }

    public DateTimeOffset? AtualizadoEm { get; set; }
}
