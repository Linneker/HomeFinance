using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.ExternalIntegration.Pagamentos;
using Acme.Sistemas.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acme.Sistemas.ExternalIntegration;

public static class ExternalIntegrationServiceCollectionExtensions
{
    /// <summary>
    /// Registra os quatro provedores de pagamento.
    /// </summary>
    /// <remarks>
    /// Entram como coleção de <see cref="IProvedorDePagamento"/>, e quem precisa de um escolhe
    /// pelo canal. Injetar cada um por tipo concreto obrigaria todo consumidor a conhecer os
    /// quatro, e acrescentar um quinto viraria uma alteração em cascata.
    /// </remarks>
    public static IServiceCollection AddExternalIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpcoesDePagamento>(
            configuration.GetSection(OpcoesDePagamento.SecaoConfiguracao));

        services.AddHttpClient(nameof(CompraInAppProvedor));
        services.AddHttpClient(nameof(GatewayDePagamento));

        Loja(services, CanalPagamento.AppStore);
        Loja(services, CanalPagamento.GooglePlay);
        Gateway(services, CanalPagamento.MercadoPago);
        Gateway(services, CanalPagamento.Asaas);

        return services;
    }

    private static void Loja(IServiceCollection services, CanalPagamento canal) =>
        services.AddScoped<IProvedorDePagamento>(sp => new CompraInAppProvedor(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CompraInAppProvedor)),
            sp.GetRequiredService<ISecretProvider>(),
            sp.GetRequiredService<IOptions<OpcoesDePagamento>>(),
            sp.GetRequiredService<ILogger<CompraInAppProvedor>>(),
            canal));

    private static void Gateway(IServiceCollection services, CanalPagamento canal) =>
        services.AddScoped<IProvedorDePagamento>(sp => new GatewayDePagamento(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GatewayDePagamento)),
            sp.GetRequiredService<ISecretProvider>(),
            sp.GetRequiredService<IOptions<OpcoesDePagamento>>(),
            sp.GetRequiredService<ILogger<GatewayDePagamento>>(),
            canal));
}
