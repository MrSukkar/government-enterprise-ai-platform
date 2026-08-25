using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Platform.Observability.OpenTelemetry;
using Platform.Observability.Redaction;
using Platform.Observability.Central;
using Platform.Observability.Collection;

namespace Platform.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformObservabilityFoundation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var collectorAgent = CollectorAgentExportProfile.FromConfiguration(configuration);
        services.AddSingleton(collectorAgent);
        services.AddSingleton(TelemetryRedactionPolicy.Strict);
        services.AddSingleton<TelemetryAttributeRedactor>();
        services.AddSingleton<RedactingActivityProcessor>();
        services.AddSingleton<PlatformTelemetry>();
        services.AddScoped<CentralObservabilityService>();

        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("GovernmentEnterpriseAIPlatform.Api"))
            .WithTracing(tracing => tracing
                .AddSource(PlatformTelemetry.ActivitySourceName)
                .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                .AddProcessor<RedactingActivityProcessor>())
            .WithMetrics(metrics => metrics
                .AddMeter(PlatformTelemetry.MeterName));

        if (collectorAgent.Enabled)
        {
            telemetry.WithTracing(tracing => tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = collectorAgent.Endpoint!;
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            }));
            telemetry.WithMetrics(metrics => metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = collectorAgent.Endpoint!;
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            }));
        }

        return services;
    }
}
