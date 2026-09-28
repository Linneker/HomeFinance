using Acme.Sistemas.Domain.Entities.Auditoria;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>
/// Grava a trilha de auditoria. Só escrita: a leitura da trilha é do back-office administrativo.
/// </summary>
public interface IAuditoriaRepository
{
    Task RegistrarAsync(RegistroAuditoriaEntity registro, CancellationToken cancellationToken);
}
