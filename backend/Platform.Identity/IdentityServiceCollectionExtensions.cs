using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Identity.Access;
using Platform.Identity.Authentication;

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
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = FailClosedAuthenticationDefaults.Scheme;
                options.DefaultChallengeScheme = FailClosedAuthenticationDefaults.Scheme;
                options.DefaultForbidScheme = FailClosedAuthenticationDefaults.Scheme;
            })
            .AddScheme<AuthenticationSchemeOptions, FailClosedAuthenticationHandler>(
                FailClosedAuthenticationDefaults.Scheme,
                _ => { });
        services.AddSingleton<IAccessPolicyEvaluator, DefaultAccessPolicyEvaluator>();
        services.AddSingleton<GovernedRequestContextFactory>();

        return services;
    }
}
