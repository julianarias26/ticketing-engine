using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TicketingEngine.Infrastructure.Observability;

public static class TelemetrySetup
{
    public const string ServiceName    = "ticketing-engine";
    public const string ServiceVersion = "1.0.0";

    public static readonly ActivitySource ActivitySource =
        new(ServiceName, ServiceVersion);

    public static readonly Meter Meter =
        new(ServiceName, ServiceVersion);

    public static IServiceCollection AddObservability(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<TicketingMetrics>();

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(ServiceName,
                serviceVersion: ServiceVersion))
            .WithTracing(t => t
                .AddSource(ServiceName)
                .AddAspNetCoreInstrumentation(o =>
                {
                    o.RecordException = true;
                    o.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(o =>
                    o.SetDbStatementForText = true)
                .AddOtlpExporter(o =>
                    o.Endpoint = new Uri(
                        config["Otel:Endpoint"] ?? "http://localhost:4317")))
            .WithMetrics(m => m
                .AddMeter(ServiceName)
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

        return services;
    }
}
