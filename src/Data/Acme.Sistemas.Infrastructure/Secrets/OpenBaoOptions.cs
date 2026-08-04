namespace Acme.Sistemas.Infrastructure.Secrets;

public sealed class OpenBaoOptions
{
    public const string SecaoConfiguracao = "OpenBao";

    /// <summary>Endereço do OpenBao, ex.: http://localhost:8200.</summary>
    public string Endereco { get; set; } = string.Empty;

    /// <summary>
    /// Token de acesso. Vem do ambiente (variável, arquivo montado ou identidade do pod) —
    /// nunca de <c>appsettings</c>.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Nome do mount do engine KV v2.</summary>
    public string Mount { get; set; } = "secret";

    /// <summary>Caminho do segredo dentro do mount.</summary>
    public string Caminho { get; set; } = "homefinance";

    /// <summary>Por quanto tempo um segredo lido fica em memória antes de ser relido.</summary>
    public TimeSpan DuracaoCache { get; set; } = TimeSpan.FromMinutes(5);
}
