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

    /// <summary>
    /// Emite o token do administrador: sem tenant, com a claim de perfil.
    /// </summary>
    /// <remarks>
    /// A ausência da claim de tenant padrão é o que garante que a requisição administrativa não
    /// selecione tenant nenhum — o administrador não tem por onde chegar ao dado do cliente. A
    /// claim de perfil é o que a política administrativa exige, e o token do usuário final não a
    /// carrega, então uma credencial não vale no lado do outro.
    /// </remarks>
    Task<TokenEmitido> EmitirAdministradorAsync(
        Guid administradorId,
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

    /// <summary>Claim de perfil. Fonte única do nome, compartilhada com a política de autorização.</summary>
    public const string ClaimPerfil = "perfil";

    /// <summary>Valor do perfil administrativo.</summary>
    public const string PerfilAdministrador = "administrador";

    private const int TamanhoMinimoChaveBytes = 64;

    private readonly JwtOptions _options = options.Value;

    public Task<TokenEmitido> EmitirAsync(
        Guid usuarioId,
        Guid tenantPadraoId,
        CancellationToken cancellationToken = default) =>
        EmitirComClaimsAsync(
            usuarioId,
            [new Claim(ClaimTenantPadrao, tenantPadraoId.ToString())],
            cancellationToken);

    public Task<TokenEmitido> EmitirAdministradorAsync(
        Guid administradorId,
        CancellationToken cancellationToken = default) =>
        EmitirComClaimsAsync(
            administradorId,
            [new Claim(ClaimPerfil, PerfilAdministrador)],
            cancellationToken);

    private async Task<TokenEmitido> EmitirComClaimsAsync(
        Guid sujeito,
        IEnumerable<Claim> claimsExtras,
        CancellationToken cancellationToken)
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

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, sujeito.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
        };
        claims.AddRange(claimsExtras);

        var descricao = new SecurityTokenDescriptor
        {
            Issuer = _options.Emissor,
            Audience = _options.Audiencia,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(chave), SecurityAlgorithms.HmacSha512),
        };

        return new TokenEmitido(new JsonWebTokenHandler().CreateToken(descricao), expiraEm);
    }
}
