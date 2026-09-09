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
        var sandboxOptions = configuration
            .GetSection(SandboxRuntimeOptions.SectionName)
            .Get<SandboxRuntimeOptions>() ?? new();
        var testsOptions = configuration.GetSection(TestsRuntimeOptions.SectionName).Get<TestsRuntimeOptions>() ?? new();
        var humanReviewOptions = configuration.GetSection(HumanReviewRuntimeOptions.SectionName).Get<HumanReviewRuntimeOptions>() ?? new();
        var gitOptions = configuration.GetSection(GitRuntimeOptions.SectionName).Get<GitRuntimeOptions>() ?? new();
        var ciCdOptions = configuration.GetSection(CiCdRuntimeOptions.SectionName).Get<CiCdRuntimeOptions>() ?? new();
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
        services.Configure<SandboxRuntimeOptions>(
            configuration.GetSection(SandboxRuntimeOptions.SectionName));
        services.Configure<TestsRuntimeOptions>(configuration.GetSection(TestsRuntimeOptions.SectionName));
        services.Configure<HumanReviewRuntimeOptions>(configuration.GetSection(HumanReviewRuntimeOptions.SectionName));
        services.Configure<GitRuntimeOptions>(configuration.GetSection(GitRuntimeOptions.SectionName));
        services.Configure<CiCdRuntimeOptions>(configuration.GetSection(CiCdRuntimeOptions.SectionName));
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
        var sandboxState =
            securityValidationState == SecurityValidationRuntimeConfigurationState.Invalid ||
            sandboxOptions.ConfigurationState == SandboxRuntimeConfigurationState.Invalid
                ? SandboxRuntimeConfigurationState.Invalid
                : securityValidationState == SecurityValidationRuntimeConfigurationState.Configured &&
                  sandboxOptions.IsOperationallyConfigured
                    ? SandboxRuntimeConfigurationState.Configured
                    : SandboxRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new SandboxRuntimeReadiness(sandboxState));
        var testsState=sandboxState==SandboxRuntimeConfigurationState.Invalid||testsOptions.ConfigurationState==TestsRuntimeConfigurationState.Invalid?TestsRuntimeConfigurationState.Invalid:sandboxState==SandboxRuntimeConfigurationState.Configured&&testsOptions.IsOperationallyConfigured?TestsRuntimeConfigurationState.Configured:TestsRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new TestsRuntimeReadiness(testsState));
        var humanReviewState=testsState==TestsRuntimeConfigurationState.Invalid||humanReviewOptions.ConfigurationState==HumanReviewRuntimeConfigurationState.Invalid?HumanReviewRuntimeConfigurationState.Invalid:testsState==TestsRuntimeConfigurationState.Configured&&humanReviewOptions.IsOperationallyConfigured?HumanReviewRuntimeConfigurationState.Configured:HumanReviewRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new HumanReviewRuntimeReadiness(humanReviewState));
        var gitState=humanReviewState==HumanReviewRuntimeConfigurationState.Invalid||gitOptions.ConfigurationState==GitRuntimeConfigurationState.Invalid?GitRuntimeConfigurationState.Invalid:humanReviewState==HumanReviewRuntimeConfigurationState.Configured&&gitOptions.IsOperationallyConfigured?GitRuntimeConfigurationState.Configured:GitRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new GitRuntimeReadiness(gitState));
        var ciCdState=gitState==GitRuntimeConfigurationState.Invalid||ciCdOptions.ConfigurationState==CiCdRuntimeConfigurationState.Invalid?CiCdRuntimeConfigurationState.Invalid:gitState==GitRuntimeConfigurationState.Configured&&ciCdOptions.IsOperationallyConfigured?CiCdRuntimeConfigurationState.Configured:CiCdRuntimeConfigurationState.Unconfigured;
        services.AddSingleton(new CiCdRuntimeReadiness(ciCdState));
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
                                if (sandboxOptions.IsOperationallyConfigured)
                                {
                                    services.AddHttpClient("sovereign-security-sandbox")
                                        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                                        {
                                            AllowAutoRedirect = false, UseCookies = false,
                                            ConnectTimeout = TimeSpan.FromSeconds(sandboxOptions.RequestTimeoutSeconds)
                                        });
                                    services.AddScoped<ISandboxPolicyGate, SovereignSandboxPolicyGate>();
                                    services.AddScoped<ISandboxSecurityValidationReceiptReader,
                                        PostgreSqlSandboxSecurityValidationReceiptReader>();
                                    services.AddScoped<ISandboxCodeGenerationCandidateReader,
                                        PostgreSqlSandboxCodeGenerationCandidateReader>();
                                    services.AddScoped<ISandboxDeliveryRunReader, PostgreSqlSandboxDeliveryRunReader>();
                                    services.AddScoped<ISecuritySandboxRuntime, SovereignHttpSecuritySandboxRuntime>();
                                    services.AddScoped<ISandboxResultAuthorizer, DeterministicSandboxResultAuthorizer>();
                                    services.AddScoped<ISandboxEvidenceRecorder, PostgreSqlSandboxEvidenceRecorder>();
                                    if (testsOptions.IsOperationallyConfigured)
                                    {
                                        services.AddHttpClient("sovereign-governed-tests").ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect=false,UseCookies=false,ConnectTimeout=TimeSpan.FromSeconds(testsOptions.RequestTimeoutSeconds) });
                                        services.AddScoped<ITestsPolicyGate,SovereignTestsPolicyGate>();
                                        services.AddScoped<ITestsSandboxExecutionReceiptReader,PostgreSqlTestsSandboxReceiptReader>();
                                        services.AddScoped<ITestsSecurityValidationReceiptReader,PostgreSqlTestsSecurityReader>();
                                        services.AddScoped<ITestsCodeGenerationCandidateReader,PostgreSqlTestsCandidateReader>();
                                        services.AddScoped<ITestsDeliveryRunReader,PostgreSqlTestsRunReader>();
                                        services.AddScoped<IGovernedTestManifestReader,PostgreSqlTestManifestReader>();
                                        services.AddScoped<IGovernedTestRuntime,SovereignHttpGovernedTestRuntime>();
                                        services.AddScoped<ITestsResultAuthorizer,DeterministicTestsResultAuthorizer>();
                                        services.AddScoped<ITestsEvidenceRecorder,PostgreSqlTestsEvidenceRecorder>();
                                        if (humanReviewOptions.IsOperationallyConfigured)
                                        {
                                            services.AddHttpClient("sovereign-human-review-attestation").ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect=false,UseCookies=false,ConnectTimeout=TimeSpan.FromSeconds(humanReviewOptions.RequestTimeoutSeconds) });
                                            services.AddScoped<IHumanReviewPolicyGate,SovereignHumanReviewPolicyGate>();
                                            services.AddScoped<IHumanReviewTestsReceiptReader,PostgreSqlHumanReviewTestsReader>();
                                            services.AddScoped<IHumanReviewSandboxReceiptReader,PostgreSqlHumanReviewSandboxReader>();
                                            services.AddScoped<IHumanReviewSecurityReceiptReader,PostgreSqlHumanReviewSecurityReader>();
                                            services.AddScoped<IHumanReviewCandidateReader,PostgreSqlHumanReviewCandidateReader>();
                                            services.AddScoped<IHumanReviewDeliveryRunReader,PostgreSqlHumanReviewRunReader>();
                                            services.AddScoped<IHumanReviewAttestationVerifier,SovereignHttpHumanReviewAttestationVerifier>();
                                            services.AddScoped<IAtomicHumanReviewRepository,PostgreSqlAtomicHumanReviewRepository>();
                                            if (gitOptions.IsOperationallyConfigured)
                                            {
                                                services.AddHttpClient("sovereign-institutional-git").ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect=false,UseCookies=false,ConnectTimeout=TimeSpan.FromSeconds(gitOptions.RequestTimeoutSeconds) });
                                                services.AddScoped<IGitPolicyGate,SovereignGitPolicyGate>();
                                                services.AddScoped<IGitHumanReviewReceiptReader,PostgreSqlGitHumanReviewReader>();
                                                services.AddScoped<IGitTestsReceiptReader,PostgreSqlGitTestsReader>();
                                                services.AddScoped<IGitCandidateReader,PostgreSqlGitCandidateReader>();
                                                services.AddScoped<IGitDeliveryRunReader,PostgreSqlGitRunReader>();
                                                services.AddScoped<SovereignHttpInstitutionalGitGateway>();
                                                services.AddScoped<IGovernedGitChangeSetMaterializer>(p=>p.GetRequiredService<SovereignHttpInstitutionalGitGateway>());
                                                services.AddScoped<IGitChangePolicyValidator>(p=>p.GetRequiredService<SovereignHttpInstitutionalGitGateway>());
                                                services.AddScoped<IInstitutionalGitGateway>(p=>p.GetRequiredService<SovereignHttpInstitutionalGitGateway>());
                                                services.AddScoped<IGitResultAuthorizer,DeterministicGitResultAuthorizer>();
                                                services.AddScoped<IGitEvidenceRecorder,PostgreSqlGitEvidenceRecorder>();
                                                if (ciCdOptions.IsOperationallyConfigured)
                                                {
                                                    services.AddHttpClient("sovereign-institutional-cicd").ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect=false,UseCookies=false,ConnectTimeout=TimeSpan.FromSeconds(ciCdOptions.RequestTimeoutSeconds) });
                                                    services.AddScoped<ICiCdPolicyGate,SovereignCiCdPolicyGate>();
                                                    services.AddScoped<IAuthorizedGitSourceCommitReceiptReader,PostgreSqlAuthorizedGitReceiptReader>();
                                                    services.AddScoped<ICiCdDeliveryRunReader,PostgreSqlCiCdRunReader>();
                                                    services.AddScoped<IGovernedCiCdWorkflowDefinitionReader,PostgreSqlCiCdWorkflowReader>();
                                                    services.AddScoped<SovereignHttpCiCdGateway>();
                                                    services.AddScoped<ICiCdWorkflowValidator>(p=>p.GetRequiredService<SovereignHttpCiCdGateway>());
                                                    services.AddScoped<IInstitutionalCiCdGateway>(p=>p.GetRequiredService<SovereignHttpCiCdGateway>());
                                                    services.AddScoped<ICiCdResultAuthorizer,DeterministicCiCdResultAuthorizer>();
                                                    services.AddScoped<ICiCdEvidenceRecorder,PostgreSqlCiCdEvidenceRecorder>();
                                                }
                                            }
                                        }
                                    }
                                }
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
