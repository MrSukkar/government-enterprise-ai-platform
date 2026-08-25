using Microsoft.Extensions.DependencyInjection;

namespace Platform.Governance;

public static class GovernanceServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformGovernanceFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<GovernedActions.GovernedActionGateway>();
        return services;
    }
}
