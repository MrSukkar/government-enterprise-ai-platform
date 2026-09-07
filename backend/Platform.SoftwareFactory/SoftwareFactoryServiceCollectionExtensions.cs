using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Platform.Governance.Policies;
using Platform.SoftwareFactory.Packages;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Sandbox;
using Platform.SoftwareFactory.Validation;
using Platform.SoftwareFactory.SupplyChain;
using Platform.SoftwareFactory.DeveloperExperience;
using Platform.SoftwareFactory.ClosedLoop;
using Platform.SoftwareFactory.VerticalSlice;
using Platform.SoftwareFactory.InternalService;
using Platform.SoftwareFactory.Persistence;

namespace Platform.SoftwareFactory;

public static class SoftwareFactoryServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformSoftwareFactoryFoundation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var persistenceOptions = configuration
            .GetSection(PostgreSqlIntentRegistrationOptions.SectionName)
            .Get<PostgreSqlIntentRegistrationOptions>() ?? new();
        var policyOptions = configuration
            .GetSection(PolicyControlPlaneOptions.SectionName)
            .Get<PolicyControlPlaneOptions>() ?? new();
        services.Configure<PostgreSqlIntentRegistrationOptions>(
            configuration.GetSection(PostgreSqlIntentRegistrationOptions.SectionName));
        services.AddSingleton(new PostgreSqlIntentRegistrationReadiness(
            persistenceOptions.ConfigurationState));
        if (persistenceOptions.IsOperationallyConfigured && policyOptions.IsOperationallyConfigured)
        {
            services.AddSingleton(_ => NpgsqlDataSource.Create(persistenceOptions.ConnectionString));
            services.AddScoped<IGovernedIntentPolicyGate, SovereignGovernedIntentPolicyGate>();
            services.AddScoped<IGovernedIntentRegistrationRepository,
                PostgreSqlGovernedIntentRegistrationRepository>();
        }
        services.AddSingleton<IPackageEligibilityEvaluator, PackageEligibilityEvaluator>();
        services.AddSingleton<ISoftwareFactoryEngine, DeterministicSoftwareFactoryEngine>();
        services.AddScoped<GovernedAiDevelopmentService>();
        services.AddScoped<CodeValidationPipeline>();
        services.AddScoped<GovernedSandboxService>();
        services.AddScoped<SupplyChainVerificationPipeline>();
        services.AddScoped<GovernedDeveloperExperienceService>();
        services.AddScoped<ClosedLoopEngine>();
        services.AddScoped<InternalServiceVerticalSliceEngine>();
        services.AddSingleton<GovernedIntentSubmissionValidator>();
        services.AddSingleton<GovernedIntentRegistrationEngine>();
        services.AddScoped<AuthorizedEnterpriseContextDiscoveryEngine>();
        services.AddScoped<AuthorizedExistingSystemsDiscoveryEngine>();
        services.AddScoped<AuthorizedExistingArchitectureDiscoveryEngine>();
        services.AddScoped<GovernedApprovedPackagesSelectionEngine>();
        services.AddScoped<GovernedAiPlanningEngine>();
        services.AddScoped<GovernedCodeGenerationEngine>();
        services.AddScoped<GovernedStaticValidationEngine>();
        services.AddScoped<GovernedSecurityValidationEngine>();
        services.AddScoped<GovernedSandboxExecutionEngine>();
        services.AddScoped<GovernedTestsExecutionEngine>();
        services.AddScoped<GovernedHumanReviewEngine>();
        services.AddScoped<GovernedGitSourceCommitEngine>();
        services.AddScoped<GovernedCiCdExecutionEngine>();
        services.AddScoped<GovernedArtifactPublicationEngine>();
        services.AddScoped<GovernedSovereignDeploymentEngine>();
        services.AddScoped<GovernedOpenTelemetryActivationEngine>();
        services.AddScoped<GovernedAutomaticRegistrationEngine>();
        services.AddScoped<GovernedEnterpriseModelContextualizationEngine>();
        services.AddScoped<GovernedEvidenceCompletionEngine>();
        return services;
    }
}
