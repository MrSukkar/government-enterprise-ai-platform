using Microsoft.Extensions.DependencyInjection;
using Platform.Modeling.Impact;
using Platform.Modeling.Simulation;

namespace Platform.Modeling;

public static class ModelingServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformModelingFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<EnterpriseImpactAnalysisEngine>();
        services.AddSingleton<EnterpriseSimulationEngine>();
        return services;
    }
}
