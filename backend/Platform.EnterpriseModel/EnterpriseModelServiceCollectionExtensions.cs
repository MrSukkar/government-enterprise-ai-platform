using Microsoft.Extensions.DependencyInjection;
using Platform.EnterpriseModel.Registration;
using Platform.EnterpriseModel.Understanding;
using Platform.EnterpriseModel.Intelligence;

namespace Platform.EnterpriseModel;

public static class EnterpriseModelServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformEnterpriseModelFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<AutomaticRegistrationEngine>();
        services.AddScoped<GovernedUnderstandingEngine>();
        services.AddScoped<ProactiveIntelligenceEngine>();
        return services;
    }
}
