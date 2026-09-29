using Acme.Sistemas.Domain.Entities.Auditoria;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>
/// Leitura da trilha de auditoria — do back-office.
/// </summary>
/// <remarks>
/// A tabela é append-only e o role da aplicação não tem SELECT nela. A leitura passa por função
/// <c>SECURITY DEFINER</c> restrita aos eventos de habilitação de uma conta.
/// </remarks>
public interface IAuditoriaReadRepository
{
    /// <summary>Histórico de habilitação/desabilitação da exportação de uma conta, do mais recente ao mais antigo.</summary>
    Task<IReadOnlyList<RegistroAuditoriaEntity>> HistoricoExportacaoAsync(
        Guid contaUsuarioId,
        CancellationToken cancellationToken);
}
