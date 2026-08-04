namespace Acme.Sistemas.Core.Security;

public sealed class JwtOptions
{
    public const string SecaoConfiguracao = "Jwt";

    public string Emissor { get; set; } = "acme.sistemas.homefinance";

    public string Audiencia { get; set; } = "acme.sistemas.homefinance.app";

    /// <summary>
    /// Chave de assinatura. Vem do OpenBao — nunca de <c>appsettings</c>. O gerador recusa
    /// chave curta: HS512 com chave menor que o tamanho do hash enfraquece a assinatura.
    /// </summary>
    public string Chave { get; set; } = string.Empty;

    /// <summary>
    /// Validade curta de propósito: o token não é revogável por si só, então o custo de um
    /// vazamento é limitado pelo tempo de vida.
    /// </summary>
    public TimeSpan ValidadeAcesso { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan ValidadeRenovacao { get; set; } = TimeSpan.FromDays(14);
}
