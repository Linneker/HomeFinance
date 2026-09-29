using Acme.Sistemas.Domain.Entities.Administracao;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>Credenciais de um administrador, o mínimo para autenticar sem carregar a entidade.</summary>
public sealed record CredenciaisAdministrativas(Guid AdministradorId, string SenhaHash);

/// <summary>
/// Acesso à identidade administrativa.
/// </summary>
/// <remarks>
/// A tabela <c>administrador</c> fica sob RLS sem policy: nem leitura nem escrita direta pelo
/// role da aplicação. As duas operações passam por função <c>SECURITY DEFINER</c> — a de
/// credenciais, porque o login acontece sem contexto de usuário, e a de provisionamento, que é
/// ação de operação e nunca é exposta por endpoint. Por isso a mesma interface serve leitura e
/// escrita: ambas usam o caminho definer, não a conexão comum de cada lado.
/// </remarks>
public interface IAdministradorRepository
{
    /// <summary>Credenciais pelo e-mail, ou <c>null</c> se não houver administrador ativo com ele.</summary>
    Task<CredenciaisAdministrativas?> ObterCredenciaisAsync(string email, CancellationToken cancellationToken);

    /// <summary>Cria ou atualiza um administrador. Caminho de provisionamento da operação.</summary>
    Task ProvisionarAsync(AdministradorEntity administrador, string senhaHash, CancellationToken cancellationToken);
}
