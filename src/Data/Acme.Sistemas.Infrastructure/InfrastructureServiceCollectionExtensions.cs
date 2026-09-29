using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;
using Acme.Sistemas.Infrastructure.FeatureFlags;
using Acme.Sistemas.Infrastructure.Messaging;
using Acme.Sistemas.Infrastructure.Secrets;
using Acme.Sistemas.Infrastructure.Storage;
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

        // Lido por caminho, em runtime, a cada consulta: é o que permite ligar/desligar o
        // recurso sem republicar. O arquivo é copiado para a saída pela subpasta FeatureFlags.
        services.AddSingleton<IFeatureFlagService>(_ =>
            new FeatureFlagService(
                Path.Combine(AppContext.BaseDirectory, "featureflags.json")));

        services.AddSingleton<IDataConfiguration, DataConfiguration>();

        // Storage e fila são singletons: seguram conexão viva (MinIO) e canal/conexão do broker,
        // resolvidos do cofre na primeira operação. Recriá-los por requisição reabriria socket a
        // cada exportação.
        services.AddSingleton<IArmazenamentoArquivos, MinioArmazenamentoArquivos>();
        services.AddSingleton<IFilaExportacao, RabbitMqFilaExportacao>();

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
