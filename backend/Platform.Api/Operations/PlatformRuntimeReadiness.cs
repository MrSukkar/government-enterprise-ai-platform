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
            ("Atomic Human Review and evidence repository", typeof(IAtomicHumanReviewRepository))
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
