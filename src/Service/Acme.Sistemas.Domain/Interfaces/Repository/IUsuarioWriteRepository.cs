using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>
/// Escrita e as leituras que decidem a escrita — todas pela conexão primária.
/// </summary>
/// <remarks>
/// <see cref="ExisteEmailAsync"/> e <see cref="ObterPorEmailAsync"/> vivem aqui, e não no
/// repositório de leitura, de propósito: validar unicidade contra réplica atrasada deixaria
/// passar duplicata. Ver blueprint Acme 2.1.0.
/// </remarks>
public interface IUsuarioWriteRepository
{
    /// <summary>Verificação de unicidade antes de criar a conta.</summary>
    Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Carrega o mínimo para autenticar. Leitura no fluxo de decisão.</summary>
    Task<CredenciaisDeAcesso?> ObterCredenciaisAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Cria conta e tenant no mesmo passo. São atômicos por natureza: conta sem tenant não
    /// consegue guardar nada, e tenant órfão não pertence a ninguém.
    /// </summary>
    Task CriarContaAsync(
        UsuarioEntity usuario,
        TenantEntity tenant,
        IReadOnlyList<ConsentimentoEntity> consentimentos,
        CancellationToken cancellationToken);

    Task RegistrarConsentimentoAsync(ConsentimentoEntity consentimento, CancellationToken cancellationToken);

    /// <summary>
    /// Exclui a conta e tudo que depende dela. Encerra os compartilhamentos concedidos, para
    /// que os destinatários percam o acesso imediatamente.
    /// </summary>
    Task ExcluirContaAsync(Guid usuarioId, CancellationToken cancellationToken);
}
