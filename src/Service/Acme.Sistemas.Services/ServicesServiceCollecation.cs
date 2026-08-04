using Microsoft.Extensions.DependencyInjection;

namespace Acme.Sistemas.Services;

/// <summary>
/// DI da camada de aplicação. A grafia do nome é herdada do padrão da plataforma e deve ser
/// mantida para não divergir dos demais projetos Acme.
/// </summary>
public static class ServicesServiceCollecation
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.TryAddTimeProvider();

        // Handlers e behaviors são descobertos por AddCore(assembly) no Program.cs.
        return services;
    }

    private static void TryAddTimeProvider(this IServiceCollection services)
    {
        // Injetar o relógio em vez de chamar DateTimeOffset.UtcNow direto é o que torna
        // testável tudo que depende de data — competência, caixa, vigência de renda.
        if (services.All(descriptor => descriptor.ServiceType != typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }
}
