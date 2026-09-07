using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Platform.Governance.Policies;

namespace Platform.Governance;

public static class GovernanceServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformGovernanceFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var policyOptions = configuration.GetSection(PolicyControlPlaneOptions.SectionName).Get<PolicyControlPlaneOptions>() ?? new();
        services.Configure<PolicyControlPlaneOptions>(configuration.GetSection(PolicyControlPlaneOptions.SectionName));
        services.AddSingleton(new PolicyControlPlaneReadiness(policyOptions.ConfigurationState));
        services.AddHttpClient<IPolicyBundleVerifier, SovereignPolicyBundleVerifier>();
        services.AddHttpClient<IOpaPolicyDecisionPoint, SovereignOpaPolicyDecisionPoint>();
        services.AddHttpClient<ISovereignPolicyEvaluationClient, SovereignPolicyEvaluationClient>();
        services.AddScoped<GovernedActions.GovernedActionGateway>();
        return services;
    }
}
