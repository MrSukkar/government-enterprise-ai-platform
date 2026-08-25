using Microsoft.Extensions.DependencyInjection;
using Platform.SoftwareFactory.Packages;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Sandbox;
using Platform.SoftwareFactory.Validation;
using Platform.SoftwareFactory.SupplyChain;
using Platform.SoftwareFactory.DeveloperExperience;
using Platform.SoftwareFactory.ClosedLoop;

namespace Platform.SoftwareFactory;

public static class SoftwareFactoryServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformSoftwareFactoryFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IPackageEligibilityEvaluator, PackageEligibilityEvaluator>();
        services.AddSingleton<ISoftwareFactoryEngine, DeterministicSoftwareFactoryEngine>();
        services.AddScoped<GovernedAiDevelopmentService>();
        services.AddScoped<CodeValidationPipeline>();
        services.AddScoped<GovernedSandboxService>();
        services.AddScoped<SupplyChainVerificationPipeline>();
        services.AddScoped<GovernedDeveloperExperienceService>();
        services.AddScoped<ClosedLoopEngine>();
        return services;
    }
}
