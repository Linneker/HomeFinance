using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;
using Acme.Sistemas.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acme.Sistemas.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpenBaoOptions>(options =>
        {
            configuration.GetSection(OpenBaoOptions.SecaoConfiguracao).Bind(options);

            // O token nunca vem de appsettings: só do ambiente. Ler da configuração aqui
            // serviria apenas para permitir que alguém o commitasse por engano.
            options.Token = Environment.GetEnvironmentVariable("OPENBAO_TOKEN") ?? string.Empty;
        });

        services.AddHttpClient<ISecretProvider, OpenBaoSecretProvider>();
        services.AddSingleton<IDataConfiguration, DataConfiguration>();
        services.AddScoped<MigrationRunner>();
        services.AddScoped<TransactionManager>();

        // Descoberta das migrations no próprio assembly: acrescentar uma migration nova passa a
        // ser criar o arquivo, sem editar registro de DI e sem risco de esquecer.
        foreach (var tipo in typeof(InfrastructureServiceCollectionExtensions).Assembly
                     .GetTypes()
                     .Where(tipo => tipo is { IsAbstract: false, IsInterface: false }
                                    && typeof(IMigration).IsAssignableFrom(tipo)))
        {
            services.AddScoped(typeof(IMigration), tipo);
        }

        return services;
    }
}
