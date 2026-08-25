using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Platform.Observability.OpenTelemetry;
using Platform.Observability.Redaction;

namespace Platform.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformObservabilityFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(TelemetryRedactionPolicy.Strict);
        services.AddSingleton<TelemetryAttributeRedactor>();
        services.AddSingleton<RedactingActivityProcessor>();
        services.AddSingleton<PlatformTelemetry>();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("GovernmentEnterpriseAIPlatform.Api"))
            .WithTracing(tracing => tracing
                .AddSource(PlatformTelemetry.ActivitySourceName)
                .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                .AddProcessor<RedactingActivityProcessor>())
            .WithMetrics(metrics => metrics
                .AddMeter(PlatformTelemetry.MeterName));

        return services;
    }
}
