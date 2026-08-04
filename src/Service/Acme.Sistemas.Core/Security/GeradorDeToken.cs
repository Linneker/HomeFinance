using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Acme.Sistemas.Core.Security;

public interface IGeradorDeToken
{
    Task<TokenEmitido> EmitirAsync(
        Guid usuarioId,
        Guid tenantPadraoId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Origem da chave de assinatura. Existe para o Core não depender da Infrastructure: quem sabe
/// ler o cofre é a camada de fora, o gerador só precisa da chave.
/// </summary>
public interface IProvedorDeChaveJwt
{
    Task<string> ObterChaveAsync(CancellationToken cancellationToken = default);
}

/// <param name="ExpiraEm">Instante de expiração, para o cliente renovar antes de falhar.</param>
public sealed record TokenEmitido(string Token, DateTimeOffset ExpiraEm);

/// <summary>Emite o token de acesso assinado em HS512.</summary>
public sealed class GeradorDeToken(
    IOptions<JwtOptions> options,
    IProvedorDeChaveJwt provedorDeChave,
    TimeProvider relogio) : IGeradorDeToken
{
    /// <summary>Claim com o tenant padrão. A seleção de outro tenant é validada por requisição.</summary>
    public const string ClaimTenantPadrao = "tenant_padrao";

    private const int TamanhoMinimoChaveBytes = 64;

    private readonly JwtOptions _options = options.Value;

    public async Task<TokenEmitido> EmitirAsync(
        Guid usuarioId,
        Guid tenantPadraoId,
        CancellationToken cancellationToken = default)
    {
        var chave = Encoding.UTF8.GetBytes(await provedorDeChave.ObterChaveAsync(cancellationToken));

        if (chave.Length < TamanhoMinimoChaveBytes)
        {
            // Falhar aqui é melhor que emitir token fraco silenciosamente.
            throw new InvalidOperationException(
                $"A chave JWT deve ter ao menos {TamanhoMinimoChaveBytes} bytes para HS512.");
        }

        var agora = relogio.GetUtcNow();
        var expiraEm = agora.Add(_options.ValidadeAcesso);

        var descricao = new SecurityTokenDescriptor
        {
            Issuer = _options.Emissor,
            Audience = _options.Audiencia,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
                new Claim(ClaimTenantPadrao, tenantPadraoId.ToString()),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(chave), SecurityAlgorithms.HmacSha512),
        };

        return new TokenEmitido(new JsonWebTokenHandler().CreateToken(descricao), expiraEm);
    }
}
