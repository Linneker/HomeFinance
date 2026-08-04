using System.Reflection;
using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Mediators.Notification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Acme.Sistemas.Core;

/// <summary>
/// Registro de DI do Core. A grafia do nome é herdada do padrão da plataforma e deve ser
/// mantida para não divergir dos demais projetos Acme.
/// </summary>
public static class DepencieInjection
{
    private static readonly Type[] OpenHandlerInterfaces =
    [
        typeof(IRequestHandler<,>),
        typeof(INotificationHandler<>),
        typeof(IPipelineBehavior<,>),
    ];

    /// <summary>
    /// Registra o mediator e descobre handlers e behaviors nos assemblies informados.
    /// </summary>
    /// <remarks>
    /// Behaviors são registrados na ordem em que aparecem no assembly, e essa ordem define o
    /// aninhamento do pipeline. Quando a ordem importar, registre-os explicitamente com
    /// <see cref="AddPipelineBehavior"/> antes de chamar este método.
    /// </remarks>
    public static IServiceCollection AddCore(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddScoped<IMediator, Mediator>();

        if (assemblies.Length == 0)
        {
            return services;
        }

        var tiposConcretos = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(tipo => tipo is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var tipo in tiposConcretos)
        {
            var interfacesFechadas = tipo.GetInterfaces()
                .Where(contrato => contrato.IsGenericType
                                   && OpenHandlerInterfaces.Contains(contrato.GetGenericTypeDefinition()));

            foreach (var contrato in interfacesFechadas)
            {
                // TryAddEnumerable e não Add: notifications aceitam vários handlers e behaviors
                // formam cadeia, então múltiplas implementações do mesmo contrato são válidas —
                // mas a MESMA implementação registrada duas vezes faria o behavior rodar duas
                // vezes. É o que aconteceria com um behavior já registrado explicitamente antes
                // desta varredura.
                services.TryAddEnumerable(ServiceDescriptor.Scoped(contrato, tipo));
            }
        }

        return services;
    }

    /// <summary>
    /// Registra um behavior explicitamente. Use quando a posição dele no pipeline importar —
    /// o primeiro registrado é o mais externo.
    /// </summary>
    public static IServiceCollection AddPipelineBehavior(
        this IServiceCollection services,
        Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        // Genérico aberto: vale para todo request (log, auditoria, transação).
        if (behaviorType.IsGenericTypeDefinition)
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), behaviorType));
            return services;
        }

        // Fechado: vale para um request específico. Registrar explicitamente é o que dá
        // controle de posição no pipeline — a varredura de assembly não garante ordem.
        var contratos = behaviorType.GetInterfaces()
            .Where(contrato => contrato.IsGenericType
                               && contrato.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToArray();

        if (contratos.Length == 0)
        {
            throw new ArgumentException(
                $"'{behaviorType.FullName}' não implementa IPipelineBehavior<,>.",
                nameof(behaviorType));
        }

        foreach (var contrato in contratos)
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped(contrato, behaviorType));
        }

        return services;
    }
}
