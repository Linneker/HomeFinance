using Acme.Sistemas.Core.Security;
using Acme.Sistemas.Infrastructure.Secrets;

namespace Acme.Sistemas.HomeFinance.Api.Config;

/// <summary>Lê a chave de assinatura do cofre. O cache curto fica no próprio provedor de segredos.</summary>
public sealed class ProvedorDeChaveJwt(ISecretProvider segredos) : IProvedorDeChaveJwt
{
    public Task<string> ObterChaveAsync(CancellationToken cancellationToken = default)
        => segredos.GetAsync(AutenticacaoConfig.ChaveSegredoJwt, cancellationToken);
}
