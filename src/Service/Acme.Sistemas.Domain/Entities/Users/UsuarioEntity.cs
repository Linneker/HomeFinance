namespace Acme.Sistemas.Domain.Entities.Users;

public sealed class UsuarioEntity : BaseEntity
{
    public required string Email { get; set; }

    public required string SenhaHash { get; set; }

    /// <summary>Preenchido na exclusão solicitada pelo titular.</summary>
    public DateTimeOffset? ExcluidoEm { get; set; }

    public bool Ativo => ExcluidoEm is null;
}
