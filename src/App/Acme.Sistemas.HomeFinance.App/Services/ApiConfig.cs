namespace Acme.Sistemas.HomeFinance.App.Services;

/// <summary>
/// Endereço base da Api. O app fala com a Api por HTTP, como o portal — não referencia as
/// camadas de servidor.
/// </summary>
/// <remarks>
/// O valor real vem da configuração de build/ambiente em produção. O padrão aponta para a Api
/// local (perfil <c>http</c>); no Android o emulador enxerga o host por <c>10.0.2.2</c>.
/// </remarks>
public sealed class ApiConfig
{
    public string BaseUrl { get; init; } =
        DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5286" : "http://localhost:5286";
}
