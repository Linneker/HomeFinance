namespace Acme.Sistemas.Infrastructure.Secrets;

/// <summary>
/// Origem de segredos da aplicação. Nenhuma connection string, chave ou credencial vem de
/// <c>appsettings</c> — regra 15 do blueprint Acme.
/// </summary>
public interface ISecretProvider
{
    /// <summary>Lê um segredo. Lança se ele não existir: rodar sem segredo é falha, não default.</summary>
    Task<string> GetAsync(string chave, CancellationToken cancellationToken = default);
}
