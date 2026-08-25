using Microsoft.Extensions.DependencyInjection;
using Platform.EnterpriseModel.Registration;

namespace Platform.EnterpriseModel;

public static class EnterpriseModelServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformEnterpriseModelFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<AutomaticRegistrationEngine>();
        return services;
    }
}
