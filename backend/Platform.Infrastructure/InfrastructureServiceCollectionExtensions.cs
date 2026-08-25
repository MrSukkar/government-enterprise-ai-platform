using Microsoft.Extensions.DependencyInjection;
using Platform.Infrastructure.Sovereignty;

namespace Platform.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformInfrastructureFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<GovernedSovereignDeploymentService>();
        return services;
    }
}
