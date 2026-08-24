using Microsoft.Extensions.DependencyInjection;
using Platform.SoftwareFactory.Packages;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.AiDevelopment;

namespace Platform.SoftwareFactory;

public static class SoftwareFactoryServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformSoftwareFactoryFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IPackageEligibilityEvaluator, PackageEligibilityEvaluator>();
        services.AddSingleton<ISoftwareFactoryEngine, DeterministicSoftwareFactoryEngine>();
        services.AddScoped<GovernedAiDevelopmentService>();
        return services;
    }
}
