using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Acme.Sistemas.HomeFinance.Api.Config;

/// <summary>
/// Instrumentação OpenTelemetry da Api. Os três sinais são obrigatórios pelo blueprint Acme
/// (regra 18) — exportar apenas log é o desvio mais comum.
/// </summary>
public static class ObservabilidadeConfig
{
    public const string NomeServico = "acme-sistemas-homefinance-api";

    public static IHostApplicationBuilder AddObservabilidade(this IHostApplicationBuilder builder)
    {
        var endpointOtlp = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

        var resource = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: NomeServico,
                serviceVersion: typeof(ObservabilidadeConfig).Assembly.GetName().Version?.ToString() ?? "0.0.0")
            .AddAttributes([new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)]);

        builder.Logging.ClearProviders();
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resource);
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(endpointOtlp));
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(exporter => exporter.Endpoint = new Uri(endpointOtlp)))
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(exporter => exporter.Endpoint = new Uri(endpointOtlp)));

        return builder;
    }
}
