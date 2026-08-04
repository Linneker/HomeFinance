namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>
/// O mínimo necessário para autenticar. Deliberadamente não carrega e-mail nem datas: o que
/// não é devolvido não vaza.
/// </summary>
public sealed record CredenciaisDeAcesso(Guid UsuarioId, string SenhaHash, Guid TenantPadraoId);
