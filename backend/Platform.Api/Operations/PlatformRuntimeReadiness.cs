using Platform.AgenticWork.Execution;
using Platform.EnterpriseModel.Intelligence;
using Platform.EnterpriseModel.Registration;
using Platform.EnterpriseModel.Understanding;
using Platform.Evidence.Chain;
using Platform.Governance.Policies;
using Platform.Infrastructure.Productization;
using Platform.Infrastructure.Sovereignty;
using Platform.Modeling.Impact;
using Platform.Modeling.Simulation;
using Platform.Observability.Central;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.ClosedLoop;
using Platform.SoftwareFactory.DeveloperExperience;
using Platform.SoftwareFactory.Sandbox;
using Platform.SoftwareFactory.VerticalSlice;
using Platform.SoftwareFactory.InternalService;
using Platform.Knowledge.Retrieval;
using Platform.Integrations.ExistingSystems;
using Platform.Integrations.ExistingArchitecture;
using Platform.SoftwareFactory.Packages;
using Platform.SoftwareFactory.Validation;
using Platform.SoftwareFactory.SupplyChain;

namespace Platform.Api.Operations;

internal sealed record PlatformRuntimeDependency(
    string Capability,
    string Contract,
    bool Registered);

internal sealed class PlatformRuntimeReadiness
{
    private PlatformRuntimeReadiness(IReadOnlyList<PlatformRuntimeDependency> dependencies)
    {
        Dependencies = dependencies;
    }

    internal IReadOnlyList<PlatformRuntimeDependency> Dependencies { get; }

    internal bool IsReady => Dependencies.All(dependency => dependency.Registered);

    internal IReadOnlyList<PlatformRuntimeDependency> MissingDependencies =>
        Dependencies.Where(dependency => !dependency.Registered).ToArray();

    internal static PlatformRuntimeReadiness Inspect(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var requiredDependencies = new (string Capability, Type Contract)[]
        {
            ("AI development runtime", typeof(IAiDevelopmentRuntime)),
            ("Security sandbox runtime", typeof(ISecuritySandboxRuntime)),
            ("Developer environment inspector", typeof(IDeveloperEnvironmentInspector)),
            ("Closed-loop context provider", typeof(IClosedLoopContextProvider)),
            ("Vertical-slice stage executor", typeof(IVerticalSliceStageExecutor)),
            ("Sovereign deployment runtime", typeof(ISovereignDeploymentRuntime)),
            ("Government product manifest verifier", typeof(IProductManifestVerifier)),
            ("Observability read backend", typeof(IObservabilityReadBackend)),
            ("Automatic registration repository", typeof(IAutomaticRegistrationRepository)),
            ("Understanding context provider", typeof(IUnderstandingContextProvider)),
            ("Proactive intelligence context provider", typeof(IProactiveIntelligenceContextProvider)),
            ("Durable agentic-work store", typeof(IDurableAgenticWorkStore)),
            ("OPA policy bundle verifier", typeof(IPolicyBundleVerifier)),
            ("Enterprise Model snapshot provider", typeof(IEnterpriseModelSnapshotProvider)),
            ("Digital-twin snapshot provider", typeof(IDigitalTwinSnapshotProvider)),
            ("Cryptographic evidence-chain store", typeof(IEvidenceChainStore)),
            ("Governed intent OPA policy gate", typeof(IGovernedIntentPolicyGate)),
            ("Governed intent atomic registration repository", typeof(IGovernedIntentRegistrationRepository)),
            ("Governed intent registration reader", typeof(IGovernedIntentRegistrationReader)),
            ("Enterprise Context OPA policy gate", typeof(IEnterpriseContextPolicyGate)),
            ("Enterprise Context retrieval source", typeof(IKnowledgeRetrievalSource)),
            ("Enterprise Context evidence recorder", typeof(IEnterpriseContextEvidenceRecorder)),
            ("Authorized Enterprise Context snapshot reader", typeof(IAuthorizedEnterpriseContextSnapshotReader)),
            ("Existing Systems OPA policy gate", typeof(IExistingSystemsPolicyGate)),
            ("Existing Systems inventory source", typeof(IExistingSystemInventorySource)),
            ("Existing Systems result authorizer", typeof(IExistingSystemResultAuthorizer)),
            ("Existing Systems evidence recorder", typeof(IExistingSystemsEvidenceRecorder)),
            ("Authorized Existing Systems snapshot reader", typeof(IAuthorizedExistingSystemsSnapshotReader)),
            ("Existing Architecture OPA policy gate", typeof(IExistingArchitecturePolicyGate)),
            ("Existing Architecture source", typeof(IExistingArchitectureSource)),
            ("Existing Architecture conformance validator", typeof(IExistingArchitectureConformanceValidator)),
            ("Existing Architecture result authorizer", typeof(IExistingArchitectureResultAuthorizer)),
            ("Existing Architecture evidence recorder", typeof(IExistingArchitectureEvidenceRecorder)),
            ("Authorized Existing Architecture snapshot reader", typeof(IAuthorizedExistingArchitectureSnapshotReader)),
            ("Approved Packages OPA policy gate", typeof(IApprovedPackagesPolicyGate)),
            ("Institutional package registry reader", typeof(IInstitutionalPackageRegistryReader)),
            ("Approved Package supply-chain verifier", typeof(IApprovedPackageSupplyChainVerifier)),
            ("Approved Package result authorizer", typeof(IApprovedPackageResultAuthorizer)),
            ("Approved Packages evidence recorder", typeof(IApprovedPackagesEvidenceRecorder)),
            ("Authorized Approved Packages snapshot reader", typeof(IAuthorizedApprovedPackagesSnapshotReader)),
            ("AI Planning delivery-run reader", typeof(IAiPlanningDeliveryRunReader)),
            ("AI Planning OPA policy gate", typeof(IAiPlanningPolicyGate)),
            ("Governed planning prompt-template reader", typeof(IGovernedPlanningPromptTemplateReader)),
            ("AI Planning context authorizer", typeof(IAiPlanningContextAuthorizer)),
            ("Independent AI output evaluator", typeof(IAiOutputEvaluator)),
            ("AI Planning result authorizer", typeof(IAiPlanningResultAuthorizer)),
            ("AI Planning evidence recorder", typeof(IAiPlanningEvidenceRecorder)),
            ("Authorized AI Planning candidate reader", typeof(IAuthorizedAiPlanningCandidateReader)),
            ("Code Generation delivery-run reader", typeof(ICodeGenerationDeliveryRunReader)),
            ("Code Generation OPA policy gate", typeof(ICodeGenerationPolicyGate)),
            ("Governed Code Generation prompt-template reader", typeof(IGovernedCodeGenerationPromptTemplateReader)),
            ("Code Generation context authorizer", typeof(ICodeGenerationContextAuthorizer)),
            ("Code Generation result and path authorizer", typeof(ICodeGenerationResultAuthorizer)),
            ("Code Generation evidence recorder", typeof(ICodeGenerationEvidenceRecorder)),
            ("Static Validation OPA policy gate", typeof(IStaticValidationPolicyGate)),
            ("Authorized Code Generation candidate reader", typeof(IAuthorizedCodeGenerationCandidateReader)),
            ("Static Validation delivery-run reader", typeof(IStaticValidationDeliveryRunReader)),
            ("Institutionally approved Static Validation controls", typeof(ICodeValidationControl)),
            ("Static Validation result authorizer", typeof(IStaticValidationResultAuthorizer)),
            ("Static Validation evidence recorder", typeof(IStaticValidationEvidenceRecorder)),
            ("Security Validation OPA policy gate", typeof(ISecurityValidationPolicyGate)),
            ("Authorized Static Validation receipt reader", typeof(IAuthorizedStaticValidationReceiptReader)),
            ("Security Validation delivery-run reader", typeof(ISecurityValidationDeliveryRunReader)),
            ("Security Validation result authorizer", typeof(ISecurityValidationResultAuthorizer)),
            ("Security Validation evidence recorder", typeof(ISecurityValidationEvidenceRecorder)),
            ("Sandbox OPA policy gate", typeof(ISandboxPolicyGate)),
            ("Authorized Security Validation receipt reader", typeof(IAuthorizedSecurityValidationReceiptReader)),
            ("Sandbox delivery-run reader", typeof(ISandboxDeliveryRunReader)),
            ("Sandbox result authorizer", typeof(ISandboxResultAuthorizer)),
            ("Sandbox evidence recorder", typeof(ISandboxEvidenceRecorder)),
            ("Tests OPA policy gate", typeof(ITestsPolicyGate)),
            ("Authorized Sandbox receipt reader", typeof(IAuthorizedSandboxExecutionReceiptReader)),
            ("Tests delivery-run reader", typeof(ITestsDeliveryRunReader)),
            ("Governed test-manifest reader", typeof(IGovernedTestManifestReader)),
            ("Governed test runtime", typeof(IGovernedTestRuntime)),
            ("Tests result authorizer", typeof(ITestsResultAuthorizer)),
            ("Tests evidence recorder", typeof(ITestsEvidenceRecorder)),
            ("Human Review OPA policy gate", typeof(IHumanReviewPolicyGate)),
            ("Authorized Tests receipt reader", typeof(IAuthorizedTestsExecutionReceiptReader)),
            ("Human Review delivery-run reader", typeof(IHumanReviewDeliveryRunReader)),
            ("Human Review attestation verifier", typeof(IHumanReviewAttestationVerifier)),
            ("Atomic Human Review and evidence repository", typeof(IAtomicHumanReviewRepository)),
            ("Git OPA policy gate", typeof(IGitPolicyGate)),
            ("Authorized Human Review receipt reader", typeof(IAuthorizedHumanReviewReceiptReader)),
            ("Git delivery-run reader", typeof(IGitDeliveryRunReader)),
            ("Governed Git change-set materializer", typeof(IGovernedGitChangeSetMaterializer)),
            ("Institutional Git change-policy validator", typeof(IGitChangePolicyValidator)),
            ("Institutional Git gateway", typeof(IInstitutionalGitGateway)),
            ("Git result authorizer", typeof(IGitResultAuthorizer)),
            ("Git evidence recorder", typeof(IGitEvidenceRecorder)),
            ("CI/CD OPA policy gate", typeof(ICiCdPolicyGate)),
            ("Authorized Git source-commit receipt reader", typeof(IAuthorizedGitSourceCommitReceiptReader)),
            ("CI/CD delivery-run reader", typeof(ICiCdDeliveryRunReader)),
            ("Governed CI/CD workflow-definition reader", typeof(IGovernedCiCdWorkflowDefinitionReader)),
            ("Institutional CI/CD workflow validator", typeof(ICiCdWorkflowValidator)),
            ("Institutional CI/CD gateway", typeof(IInstitutionalCiCdGateway)),
            ("CI/CD result authorizer", typeof(ICiCdResultAuthorizer)),
            ("CI/CD evidence recorder", typeof(ICiCdEvidenceRecorder)),
            ("Artifact OPA policy gate", typeof(IArtifactPolicyGate)),
            ("Authorized CI/CD execution receipt reader", typeof(IAuthorizedCiCdExecutionReceiptReader)),
            ("Authorized pipeline-output manifest reader", typeof(IAuthorizedPipelineOutputManifestReader)),
            ("Artifact delivery-run reader", typeof(IArtifactDeliveryRunReader)),
            ("Institutional Artifact package validator", typeof(IArtifactPackageValidator)),
            ("Institutional Artifact registry gateway", typeof(IInstitutionalArtifactRegistryGateway)),
            ("Artifact supply-chain control verifiers", typeof(ISupplyChainControlVerifier)),
            ("Artifact result authorizer", typeof(IArtifactResultAuthorizer)),
            ("Artifact evidence recorder", typeof(IArtifactEvidenceRecorder)),
            ("Deployment OPA policy gate", typeof(IDeploymentPolicyGate)),
            ("Authorized Artifact publication receipt reader", typeof(IAuthorizedArtifactPublicationReceiptReader)),
            ("Authorized verified Deployment Artifact reader", typeof(IAuthorizedDeploymentArtifactReader)),
            ("Deployment delivery-run reader", typeof(IDeploymentDeliveryRunReader)),
            ("Governed sovereign Deployment profile reader", typeof(IGovernedSovereignDeploymentProfileReader)),
            ("Institutional Deployment preflight validator", typeof(IInstitutionalDeploymentPreflightValidator)),
            ("Institutional sovereign Deployment gateway", typeof(IInstitutionalSovereignDeploymentGateway)),
            ("Deployment result authorizer", typeof(IDeploymentResultAuthorizer)),
            ("Deployment evidence recorder", typeof(IDeploymentEvidenceRecorder)),
            ("OpenTelemetry OPA policy gate", typeof(IOpenTelemetryPolicyGate)),
            ("Authorized sovereign Deployment receipt reader", typeof(IAuthorizedSovereignDeploymentReceiptReader)),
            ("OpenTelemetry delivery-run reader", typeof(IOpenTelemetryDeliveryRunReader)),
            ("Governed OpenTelemetry profile reader", typeof(IGovernedOpenTelemetryProfileReader)),
            ("OpenTelemetry redaction-policy verifier", typeof(IOpenTelemetryRedactionPolicyVerifier)),
            ("Institutional OpenTelemetry gateway", typeof(IInstitutionalOpenTelemetryGateway)),
            ("OpenTelemetry result authorizer", typeof(IOpenTelemetryResultAuthorizer)),
            ("OpenTelemetry evidence recorder", typeof(IOpenTelemetryEvidenceRecorder))
            ,("Automatic Registration OPA policy gate", typeof(IAutomaticRegistrationPolicyGate))
            ,("Authorized OpenTelemetry activation receipt reader", typeof(IAuthorizedOpenTelemetryActivationReceiptReader))
            ,("Automatic Registration delivery-run reader", typeof(IAutomaticRegistrationDeliveryRunReader))
            ,("Governed Automatic Registration manifest reader", typeof(IGovernedAutomaticRegistrationManifestReader))
            ,("Automatic Registration result authorizer", typeof(IAutomaticRegistrationResultAuthorizer))
            ,("Automatic Registration evidence recorder", typeof(IAutomaticRegistrationEvidenceRecorder))
            ,("Enterprise Model contextualization OPA policy gate", typeof(IEnterpriseModelContextPolicyGate))
            ,("Authorized Automatic Registration receipt reader", typeof(IAuthorizedAutomaticRegistrationReceiptReader))
            ,("Enterprise Model contextualization delivery-run reader", typeof(IEnterpriseModelDeliveryRunReader))
            ,("Authorized registered Enterprise Object reader", typeof(IAuthorizedRegisteredEnterpriseObjectReader))
            ,("Enterprise Model contextualization result authorizer", typeof(IEnterpriseModelContextResultAuthorizer))
            ,("Enterprise Model contextualization evidence recorder", typeof(IEnterpriseModelContextEvidenceRecorder))
        };

        var dependencies = requiredDependencies
            .Select(requirement => new PlatformRuntimeDependency(
                requirement.Capability,
                requirement.Contract.FullName ?? requirement.Contract.Name,
                services.Any(descriptor => descriptor.ServiceType == requirement.Contract)))
            .ToArray();

        return new PlatformRuntimeReadiness(dependencies);
    }
}
