using Acme.Sistemas.Domain.Entities.Users;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>
/// Leituras que atendem Queries. Usa a conexão de leitura — pode ser réplica.
/// </summary>
/// <remarks>
/// Nunca injetar em CommandHandler: decisão de escrita lê pelo repositório de escrita, pelo
/// primário. Ver blueprint Acme 2.1.0.
/// </remarks>
public interface IUsuarioReadRepository
{
    Task<UsuarioEntity?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConsentimentoEntity>> ObterConsentimentosAsync(
        Guid usuarioId,
        CancellationToken cancellationToken);
}
