namespace Acme.Sistemas.ExternalIntegration.Pagamentos;

/// <summary>
/// Endereços dos provedores.
/// </summary>
/// <remarks>
/// Só URLs. As credenciais vêm do OpenBao pelas chaves declaradas em
/// <see cref="ChavesDeSegredo"/> — regra 15 do blueprint: nenhuma credencial em
/// <c>appsettings</c>, onde ela acabaria commitada.
/// </remarks>
public sealed class OpcoesDePagamento
{
    public const string SecaoConfiguracao = "Pagamentos";

    public string AppStoreUrl { get; set; } = "https://buy.itunes.apple.com/verifyReceipt";

    public string GooglePlayUrl { get; set; } =
        "https://androidpublisher.googleapis.com/androidpublisher/v3";

    public string MercadoPagoUrl { get; set; } = "https://api.mercadopago.com";

    public string AsaasUrl { get; set; } = "https://api.asaas.com/v3";
}

/// <summary>Chaves dos segredos no cofre. Centralizadas para não divergirem entre adaptadores.</summary>
public static class ChavesDeSegredo
{
    public const string AppStore = "pagamentos/appstore";

    public const string GooglePlay = "pagamentos/googleplay";

    public const string MercadoPago = "pagamentos/mercadopago";

    public const string Asaas = "pagamentos/asaas";
}
