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
            ("Governed intent atomic registration repository", typeof(IGovernedIntentRegistrationRepository))
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
