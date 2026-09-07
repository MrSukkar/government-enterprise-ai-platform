using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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

        var providerOptions = configuration.GetSection(IdentityProviderOptions.SectionName).Get<IdentityProviderOptions>() ?? new();
        services.Configure<IdentityProviderOptions>(configuration.GetSection(IdentityProviderOptions.SectionName));
        services.AddSingleton(new IdentityControlPlaneReadiness(providerOptions.ConfigurationState));
        var scheme = providerOptions.IsOperationallyConfigured
            ? JwtBearerDefaults.AuthenticationScheme
            : FailClosedAuthenticationDefaults.Scheme;
        var authentication = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = scheme;
                options.DefaultChallengeScheme = scheme;
                options.DefaultForbidScheme = scheme;
            });
        authentication.AddScheme<AuthenticationSchemeOptions, FailClosedAuthenticationHandler>(
                FailClosedAuthenticationDefaults.Scheme,
                _ => { });
        if (providerOptions.IsOperationallyConfigured)
        {
            authentication.AddJwtBearer(options =>
            {
                options.Authority = providerOptions.Authority.TrimEnd('/');
                options.Audience = providerOptions.Audience;
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        try { _ = new GovernedRequestContextFactory().Create(context.Principal!); }
                        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or UnauthorizedAccessException)
                        { context.Fail("The authenticated principal does not satisfy the governed claim contract."); }
                        return Task.CompletedTask;
                    }
                };
            });
        }
        services.AddSingleton<IAccessPolicyEvaluator, DefaultAccessPolicyEvaluator>();
        services.AddSingleton<GovernedRequestContextFactory>();

        return services;
    }
}
