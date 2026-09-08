using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Platform.Governance.Policies;
using Platform.Knowledge.Retrieval;
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
using Platform.Integrations.ExistingSystems;
using Platform.Integrations.ExistingArchitecture;

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
        var graphOptions = configuration
            .GetSection(Neo4jEnterpriseGraphOptions.SectionName)
            .Get<Neo4jEnterpriseGraphOptions>() ?? new();
        var packageTrustOptions = configuration
            .GetSection(ApprovedPackagesTrustOptions.SectionName)
            .Get<ApprovedPackagesTrustOptions>() ?? new();
        var aiPlanningOptions = configuration
            .GetSection(AiPlanningRuntimeOptions.SectionName)
            .Get<AiPlanningRuntimeOptions>() ?? new();
        var codeGenerationOptions = configuration
            .GetSection(CodeGenerationRuntimeOptions.SectionName)
            .Get<CodeGenerationRuntimeOptions>() ?? new();
        var staticValidationOptions = configuration
            .GetSection(StaticValidationRuntimeOptions.SectionName)
            .Get<StaticValidationRuntimeOptions>() ?? new();
        var securityValidationOptions = configuration
            .GetSection(SecurityValidationRuntimeOptions.SectionName)
            .Get<SecurityValidationRuntimeOptions>() ?? new();
        services.Configure<PostgreSqlIntentRegistrationOptions>(
            configuration.GetSection(PostgreSqlIntentRegistrationOptions.SectionName));
        services.Configure<ApprovedPackagesTrustOptions>(
            configuration.GetSection(ApprovedPackagesTrustOptions.SectionName));
        services.Configure<AiPlanningRuntimeOptions>(
            configuration.GetSection(AiPlanningRuntimeOptions.SectionName));
        services.Configure<CodeGenerationRuntimeOptions>(
            configuration.GetSection(CodeGenerationRuntimeOptions.SectionName));
        services.Configure<StaticValidationRuntimeOptions>(
            configuration.GetSection(StaticValidationRuntimeOptions.SectionName));
        services.Configure<SecurityValidationRuntimeOptions>(
            configuration.GetSection(SecurityValidationRuntimeOptions.SectionName));
        services.AddSingleton(new PostgreSqlIntentRegistrationReadiness(
            persistenceOptions.ConfigurationState));
        var enterpriseContextState =
            persistenceOptions.ConfigurationState == PostgreSqlIntentRegistrationConfigurationState.Invalid ||
            policyOptions.ConfigurationState == PolicyControlPlaneConfigurationState.Invalid ||
            graphOptions.ConfigurationState == Neo4jEnterpriseGraphConfigurationState.Invalid
                ? EnterpriseContextRuntimeConfigurationState.Invalid
                : persistenceOptions.IsOperationallyConfigured && policyOptions.IsOperationallyConfigured &&
                  graphOptions.IsOperationallyConfigured
                    ? EnterpriseContextRuntimeConfigurationState.Configured
                    : EnterpriseContextRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new EnterpriseContextRuntimeReadiness(enterpriseContextState));
        var existingSystemsState =
            persistenceOptions.ConfigurationState == PostgreSqlIntentRegistrationConfigurationState.Invalid ||
            policyOptions.ConfigurationState == PolicyControlPlaneConfigurationState.Invalid ||
            graphOptions.ConfigurationState == Neo4jEnterpriseGraphConfigurationState.Invalid
                ? ExistingSystemsRuntimeConfigurationState.Invalid
                : persistenceOptions.IsOperationallyConfigured && policyOptions.IsOperationallyConfigured &&
                  graphOptions.IsOperationallyConfigured
                    ? ExistingSystemsRuntimeConfigurationState.Configured
                    : ExistingSystemsRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new ExistingSystemsRuntimeReadiness(existingSystemsState));
        var existingArchitectureState =
            persistenceOptions.ConfigurationState == PostgreSqlIntentRegistrationConfigurationState.Invalid ||
            policyOptions.ConfigurationState == PolicyControlPlaneConfigurationState.Invalid ||
            graphOptions.ConfigurationState == Neo4jEnterpriseGraphConfigurationState.Invalid
                ? ExistingArchitectureRuntimeConfigurationState.Invalid
                : persistenceOptions.IsOperationallyConfigured && policyOptions.IsOperationallyConfigured &&
                  graphOptions.IsOperationallyConfigured
                    ? ExistingArchitectureRuntimeConfigurationState.Configured
                    : ExistingArchitectureRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new ExistingArchitectureRuntimeReadiness(existingArchitectureState));
        var approvedPackagesState =
            persistenceOptions.ConfigurationState == PostgreSqlIntentRegistrationConfigurationState.Invalid ||
            policyOptions.ConfigurationState == PolicyControlPlaneConfigurationState.Invalid ||
            packageTrustOptions.ConfigurationState == ApprovedPackagesTrustConfigurationState.Invalid
                ? ApprovedPackagesRuntimeConfigurationState.Invalid
                : persistenceOptions.IsOperationallyConfigured && policyOptions.IsOperationallyConfigured &&
                  packageTrustOptions.IsOperationallyConfigured
                    ? ApprovedPackagesRuntimeConfigurationState.Configured
                    : ApprovedPackagesRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new ApprovedPackagesRuntimeReadiness(approvedPackagesState));
        var aiPlanningState =
            persistenceOptions.ConfigurationState == PostgreSqlIntentRegistrationConfigurationState.Invalid ||
            policyOptions.ConfigurationState == PolicyControlPlaneConfigurationState.Invalid ||
            packageTrustOptions.ConfigurationState == ApprovedPackagesTrustConfigurationState.Invalid ||
            aiPlanningOptions.ConfigurationState == AiPlanningRuntimeConfigurationState.Invalid
                ? AiPlanningRuntimeConfigurationState.Invalid
                : persistenceOptions.IsOperationallyConfigured && policyOptions.IsOperationallyConfigured &&
                  packageTrustOptions.IsOperationallyConfigured && aiPlanningOptions.IsOperationallyConfigured
                    ? AiPlanningRuntimeConfigurationState.Configured
                    : AiPlanningRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new AiPlanningRuntimeReadiness(aiPlanningState));
        var codeGenerationState =
            aiPlanningState == AiPlanningRuntimeConfigurationState.Invalid ||
            codeGenerationOptions.ConfigurationState == CodeGenerationRuntimeConfigurationState.Invalid
                ? CodeGenerationRuntimeConfigurationState.Invalid
                : aiPlanningState == AiPlanningRuntimeConfigurationState.Configured &&
                  codeGenerationOptions.IsOperationallyConfigured
                    ? CodeGenerationRuntimeConfigurationState.Configured
                    : CodeGenerationRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new CodeGenerationRuntimeReadiness(codeGenerationState));
        var staticValidationState =
            codeGenerationState == CodeGenerationRuntimeConfigurationState.Invalid ||
            staticValidationOptions.ConfigurationState == StaticValidationRuntimeConfigurationState.Invalid
                ? StaticValidationRuntimeConfigurationState.Invalid
                : codeGenerationState == CodeGenerationRuntimeConfigurationState.Configured &&
                  staticValidationOptions.IsOperationallyConfigured
                    ? StaticValidationRuntimeConfigurationState.Configured
                    : StaticValidationRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new StaticValidationRuntimeReadiness(staticValidationState));
        var hasOverlappingValidationControls = staticValidationOptions.Controls.Select(item => item.ControlId)
            .Intersect(securityValidationOptions.Controls.Select(item => item.ControlId), StringComparer.Ordinal).Any();
        var securityValidationState =
            staticValidationState == StaticValidationRuntimeConfigurationState.Invalid ||
            securityValidationOptions.ConfigurationState == SecurityValidationRuntimeConfigurationState.Invalid ||
            hasOverlappingValidationControls
                ? SecurityValidationRuntimeConfigurationState.Invalid
                : staticValidationState == StaticValidationRuntimeConfigurationState.Configured &&
                  securityValidationOptions.IsOperationallyConfigured
                    ? SecurityValidationRuntimeConfigurationState.Configured
                    : SecurityValidationRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new SecurityValidationRuntimeReadiness(securityValidationState));
        if (persistenceOptions.IsOperationallyConfigured && policyOptions.IsOperationallyConfigured)
        {
            services.AddSingleton(_ => NpgsqlDataSource.Create(persistenceOptions.ConnectionString));
            services.AddScoped<PostgreSqlGovernedIntentRegistrationRepository>();
            services.AddScoped<IGovernedIntentPolicyGate, SovereignGovernedIntentPolicyGate>();
            services.AddScoped<IGovernedIntentRegistrationRepository>(provider =>
                provider.GetRequiredService<PostgreSqlGovernedIntentRegistrationRepository>());
            if (packageTrustOptions.IsOperationallyConfigured)
            {
                services.AddScoped<IAuthorizedExistingArchitectureSnapshotReader,
                    PostgreSqlAuthorizedExistingArchitectureSnapshotReader>();
                services.AddScoped<IApprovedPackagesPolicyGate, SovereignApprovedPackagesPolicyGate>();
                services.AddScoped<IInstitutionalPackageRegistryReader,
                    PostgreSqlInstitutionalPackageRegistryReader>();
                services.AddScoped<IApprovedPackageSupplyChainVerifier,
                    CryptographicApprovedPackageSupplyChainVerifier>();
                services.AddScoped<IApprovedPackageResultAuthorizer,
                    DeterministicApprovedPackageResultAuthorizer>();
                services.AddScoped<IApprovedPackagesEvidenceRecorder,
                    PostgreSqlApprovedPackagesEvidenceRecorder>();
                if (aiPlanningOptions.IsOperationallyConfigured)
                {
                    services.AddHttpClient("sovereign-ai-planning-generation")
                        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                        {
                            AllowAutoRedirect = false,
                            UseCookies = false,
                            ConnectTimeout = TimeSpan.FromSeconds(aiPlanningOptions.RequestTimeoutSeconds)
                        });
                    services.AddHttpClient("sovereign-ai-planning-evaluation")
                        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                        {
                            AllowAutoRedirect = false,
                            UseCookies = false,
                            ConnectTimeout = TimeSpan.FromSeconds(aiPlanningOptions.RequestTimeoutSeconds)
                        });
                    services.AddScoped<IAuthorizedApprovedPackagesSnapshotReader,
                        PostgreSqlAuthorizedApprovedPackagesSnapshotReader>();
                    services.AddScoped<IAiPlanningDeliveryRunReader, PostgreSqlAiPlanningDeliveryRunReader>();
                    services.AddScoped<IAiPlanningPolicyGate, SovereignAiPlanningPolicyGate>();
                    services.AddScoped<IGovernedPlanningPromptTemplateReader,
                        PostgreSqlGovernedPlanningPromptTemplateReader>();
                    services.AddScoped<IAiPlanningContextAuthorizer, PostgreSqlAiPlanningContextAuthorizer>();
                    services.AddScoped<IAiDevelopmentRuntime, SovereignHttpAiPlanningRuntime>();
                    services.AddScoped<IAiOutputEvaluator, SovereignHttpAiOutputEvaluator>();
                    services.AddScoped<IAiPlanningResultAuthorizer, DeterministicAiPlanningResultAuthorizer>();
                    services.AddScoped<IAiPlanningEvidenceRecorder, PostgreSqlAiPlanningEvidenceRecorder>();
                    if (codeGenerationOptions.IsOperationallyConfigured)
                    {
                        services.AddHttpClient("sovereign-code-generation")
                            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                            {
                                AllowAutoRedirect = false, UseCookies = false,
                                ConnectTimeout = TimeSpan.FromSeconds(codeGenerationOptions.RequestTimeoutSeconds)
                            });
                        services.AddHttpClient("sovereign-code-generation-evaluation")
                            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                            {
                                AllowAutoRedirect = false, UseCookies = false,
                                ConnectTimeout = TimeSpan.FromSeconds(codeGenerationOptions.RequestTimeoutSeconds)
                            });
                        services.AddScoped<IAuthorizedAiPlanningCandidateReader, PostgreSqlAuthorizedAiPlanningCandidateReader>();
                        services.AddScoped<ICodeGenerationDeliveryRunReader, PostgreSqlCodeGenerationDeliveryRunReader>();
                        services.AddScoped<ICodeGenerationPolicyGate, SovereignCodeGenerationPolicyGate>();
                        services.AddScoped<IGovernedCodeGenerationPromptTemplateReader,
                            PostgreSqlGovernedCodeGenerationPromptTemplateReader>();
                        services.AddScoped<ICodeGenerationContextAuthorizer, PostgreSqlCodeGenerationContextAuthorizer>();
                        services.AddScoped<ICodeGenerationAiDevelopmentRuntime, SovereignHttpCodeGenerationRuntime>();
                        services.AddScoped<ICodeGenerationAiOutputEvaluator, SovereignHttpCodeGenerationEvaluator>();
                        services.AddScoped<ICodeGenerationResultAuthorizer, DeterministicCodeGenerationResultAuthorizer>();
                        services.AddScoped<ICodeGenerationEvidenceRecorder, PostgreSqlCodeGenerationEvidenceRecorder>();
                        if (staticValidationOptions.IsOperationallyConfigured)
                        {
                            services.AddScoped<IStaticValidationPolicyGate, SovereignStaticValidationPolicyGate>();
                            services.AddScoped<IStaticValidationCodeGenerationCandidateReader,
                                PostgreSqlAuthorizedCodeGenerationCandidateReader>();
                            services.AddScoped<IStaticValidationDeliveryRunReader,
                                PostgreSqlStaticValidationDeliveryRunReader>();
                            foreach (var profile in staticValidationOptions.Controls)
                                services.AddSingleton<ICodeValidationControl>(
                                    new SignedDeterministicStaticValidationControl(profile, staticValidationOptions));
                            services.AddScoped<IStaticValidationResultAuthorizer,
                                DeterministicStaticValidationResultAuthorizer>();
                            services.AddScoped<IStaticValidationEvidenceRecorder,
                                PostgreSqlStaticValidationEvidenceRecorder>();
                            if (securityValidationOptions.IsOperationallyConfigured && !hasOverlappingValidationControls)
                            {
                                services.AddScoped<ISecurityValidationPolicyGate, SovereignSecurityValidationPolicyGate>();
                                services.AddScoped<IAuthorizedStaticValidationReceiptReader,
                                    PostgreSqlAuthorizedStaticValidationReceiptReader>();
                                services.AddScoped<ISecurityValidationCodeGenerationCandidateReader,
                                    PostgreSqlSecurityValidationCodeGenerationCandidateReader>();
                                services.AddScoped<ISecurityValidationDeliveryRunReader,
                                    PostgreSqlSecurityValidationDeliveryRunReader>();
                                foreach (var profile in securityValidationOptions.Controls)
                                    services.AddSingleton<ICodeValidationControl>(
                                        new SignedDeterministicSecurityValidationControl(profile, securityValidationOptions));
                                services.AddScoped<ISecurityValidationResultAuthorizer,
                                    DeterministicSecurityValidationResultAuthorizer>();
                                services.AddScoped<ISecurityValidationEvidenceRecorder,
                                    PostgreSqlSecurityValidationEvidenceRecorder>();
                            }
                        }
                    }
                }
            }
            if (graphOptions.IsOperationallyConfigured)
            {
                services.AddScoped<IGovernedIntentRegistrationReader>(provider =>
                    provider.GetRequiredService<PostgreSqlGovernedIntentRegistrationRepository>());
                services.AddScoped<IEnterpriseContextPolicyGate, SovereignEnterpriseContextPolicyGate>();
                services.AddScoped<IEnterpriseContextEvidenceRecorder,
                    PostgreSqlEnterpriseContextEvidenceRecorder>();
                services.AddScoped<IKnowledgeRetrievalSource, Neo4jEnterpriseGraphRetrievalSource>();
                services.AddScoped<PostgreSqlAuthorizedEnterpriseContextSnapshotReader>();
                services.AddScoped<IAuthorizedEnterpriseContextSnapshotReader>(provider =>
                    provider.GetRequiredService<PostgreSqlAuthorizedEnterpriseContextSnapshotReader>());
                services.AddScoped<IExistingSystemsPolicyGate, SovereignExistingSystemsPolicyGate>();
                services.AddScoped<IExistingSystemInventorySource, Neo4jExistingSystemInventorySource>();
                services.AddScoped<IExistingSystemResultAuthorizer,
                    DeterministicExistingSystemResultAuthorizer>();
                services.AddScoped<IExistingSystemsEvidenceRecorder,
                    PostgreSqlExistingSystemsEvidenceRecorder>();
                services.AddScoped<IAuthorizedExistingSystemsSnapshotReader,
                    PostgreSqlAuthorizedExistingSystemsSnapshotReader>();
                services.AddScoped<IExistingArchitecturePolicyGate,
                    SovereignExistingArchitecturePolicyGate>();
                services.AddScoped<IExistingArchitectureConformanceValidator,
                    DeterministicExistingArchitectureConformanceValidator>();
                services.AddScoped<IExistingArchitectureSource, Neo4jExistingArchitectureSource>();
                services.AddScoped<IExistingArchitectureResultAuthorizer,
                    DeterministicExistingArchitectureResultAuthorizer>();
                services.AddScoped<IExistingArchitectureEvidenceRecorder,
                    PostgreSqlExistingArchitectureEvidenceRecorder>();
            }
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
