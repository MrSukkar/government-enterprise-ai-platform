using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Identity.Access;

namespace Platform.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformIdentityFoundation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<IdentityProviderOptions>(configuration.GetSection(IdentityProviderOptions.SectionName));
        services.AddSingleton<IAccessPolicyEvaluator, DefaultAccessPolicyEvaluator>();

        return services;
    }
}
