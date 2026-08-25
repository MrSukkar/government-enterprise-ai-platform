using Microsoft.Extensions.DependencyInjection;
using Platform.EnterpriseModel.Registration;
using Platform.EnterpriseModel.Understanding;

namespace Platform.EnterpriseModel;

public static class EnterpriseModelServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformEnterpriseModelFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<AutomaticRegistrationEngine>();
        services.AddScoped<GovernedUnderstandingEngine>();
        return services;
    }
}
