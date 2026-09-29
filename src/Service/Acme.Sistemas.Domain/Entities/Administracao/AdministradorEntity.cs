namespace Acme.Sistemas.Domain.Entities.Administracao;

/// <summary>
/// Identidade da operação do produto. Separada da do usuário final e sem vínculo com tenant: o
/// administrador administra contas, não possui uma.
/// </summary>
public sealed class AdministradorEntity : BaseEntity
{
    public required string Email { get; set; }

    public required string Nome { get; set; }

    public required string SenhaHash { get; set; }

    public DateTimeOffset? ExcluidoEm { get; set; }

    public bool Ativo => ExcluidoEm is null;
}
