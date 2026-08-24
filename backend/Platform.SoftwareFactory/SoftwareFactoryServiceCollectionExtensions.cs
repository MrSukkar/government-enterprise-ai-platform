using Microsoft.Extensions.DependencyInjection;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory;

public static class SoftwareFactoryServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformSoftwareFactoryFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IPackageEligibilityEvaluator, PackageEligibilityEvaluator>();
        return services;
    }
}
