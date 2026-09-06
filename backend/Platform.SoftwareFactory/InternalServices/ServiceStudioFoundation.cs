using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.InternalService;

public sealed record InternalServiceDeliveryStage(
    int Ordinal,
    string Key,
    string Name,
    string ControlOutcome);

public sealed record InternalServiceFoundation(
    string ProductId,
    string ProductName,
    string Increment,
    string Status,
    string ProductPromise,
    IReadOnlyList<InternalServiceDeliveryStage> DeliveryStages,
    IReadOnlyList<string> NonRegressionGates);

public static class InternalServiceFoundationCatalog
{
    public static InternalServiceFoundation Current { get; } = new(
        "sovereign-internal-services",
        "Create Internal Service Workspace",
        "Operational Increment 16 - Governed CI/CD Execution",
        "Governed CI/CD contract available; all workflow, runner, authorization, and evidence adapters fail-closed",
        "Create an internal government service through governed context, delivery, operations, and proof.",
        Enum.GetValues<DeliveryStage>()
            .Select((stage, ordinal) => new InternalServiceDeliveryStage(
                ordinal + 1,
                stage.ToString(),
                DisplayName(stage),
                ControlOutcome(stage)))
            .ToArray(),
        [
            "Development startup is smoke-tested",
            "Readiness remains fail-closed",
            "OpenAPI contract is available",
            "Anonymous intent submission is rejected with a bearer challenge",
            "Validated intent is never persisted or executed",
            "Registration requires an OPA policy gate and atomic evidence-bearing repository",
            "Missing registration adapters return service unavailable without mutation",
            "Enterprise Context policy establishes explicit scope before source access",
            "Every context candidate is re-authorized before evidence-bearing release",
            "Missing discovery adapters return service unavailable without source access",
            "Existing Systems inventory is bound to an evidence-bearing Enterprise Context snapshot",
            "Every system and relationship is structurally validated and re-authorized",
            "Live connectors, credentials, network probes, and Enterprise Model mutation remain unavailable",
            "Existing Architecture discovery is bound to an evidence-bearing Existing Systems snapshot",
            "Only approved, constitutionally conformant architecture facts can be released",
            "Missing architecture adapters return service unavailable without source access",
            "Approved Packages selection accepts only exact immutable coordinates",
            "Eligibility, provenance, SBOM, signature, and sovereign registry assurance are mandatory",
            "Package transfer, execution, registry mutation, and AI Planning remain unavailable",
            "AI Planning requires a delivery run stopped at ApprovedPackages",
            "OPA, governed prompt, re-authorized context, and exact packages precede AI invocation",
            "Independent evaluation and result authorization precede non-executable release",
            "Code Generation requires an evidence-bearing AI Planning candidate and a run stopped at AiPlanning",
            "OPA, governed prompt, re-authorized context, exact packages, and safe relative paths precede generation",
            "Generated content and paths remain inert data with no filesystem, tool, command, or Git access",
            "Independent evaluation and result authorization precede non-executable and unapplied release",
            "Code Generation candidates remain inert without filesystem, tool, command, or Git access",
            "Static Validation requires a code candidate and delivery run stopped at CodeGeneration",
            "OPA authorizes the exact candidate and Static controls before candidate read or validation",
            "Missing, incomplete, unevidenced, Error, or Critical control results fail closed",
            "No source mutation, code execution, workflow advancement, or Security Validation is available",
            "Security Validation requires accepted Static evidence and a run stopped at StaticValidation",
            "OPA authorizes the exact candidate, Static report, and Security controls before reads",
            "Missing, incomplete, unevidenced, Error, or Critical Security results fail closed",
            "No Sandbox execution, workflow advancement, or material action is available",
            "Sandbox requires accepted Security evidence and a run stopped at SecurityValidation",
            "OPA authorizes the exact image, isolation, environment, and network scope before reads or execution",
            "Only a supply-chain-assured institutional image may run in Firecracker-class ephemeral isolation",
            "Sandbox results require authorization and evidence and cannot advance to Tests or production",
            "Tests require accepted Sandbox evidence and a delivery run stopped at Sandbox",
            "OPA authorizes the exact manifest, image, isolation, environment, and network scope before tests",
            "Every required test must be discovered, completed, passed, unique, and evidence-bearing",
            "Tests results require authorization and evidence and cannot advance to Human Review or production",
            "Human Review requires accepted Tests evidence and a delivery run stopped at Tests",
            "OPA authorizes the exact human reviewer, decision package, and conflict scope before reads",
            "Reviewer identity, separation of duties, rationale, and non-repudiable attestation are mandatory",
            "Review decision and evidence are atomic and cannot advance to Git or production",
            "Git requires an approving Human Review and a delivery run stopped at HumanReview",
            "OPA authorizes the exact repository, clean base, non-protected branch, change set, and commit metadata",
            "Only the exact validated change set may become one signed immutable commit",
            "Git cannot force-update, create a PR, trigger CI/CD, affect production, or advance workflow",
            "CI/CD requires a signed Git receipt and a delivery run stopped at Git",
            "OPA authorizes the exact immutable workflow, isolated runner, stages, and controls before checkout",
            "Locked dependencies, SBOM, provenance, attestations, signatures, and exact evidence are mandatory",
            "CI/CD cannot mutate source, publish an Artifact, deploy, affect production, or advance workflow",
            "Developer console is available",
            "All 15 projects build with zero warnings and zero errors"
        ]);

    private static string DisplayName(DeliveryStage stage) => stage switch
    {
        DeliveryStage.AiPlanning => "AI planning",
        DeliveryStage.CiCd => "CI/CD",
        DeliveryStage.OpenTelemetry => "OpenTelemetry",
        _ => string.Concat(stage.ToString().Select((value, index) =>
            index > 0 && char.IsUpper(value) ? $" {value}" : value.ToString()))
    };

    private static string ControlOutcome(DeliveryStage stage) => stage switch
    {
        DeliveryStage.Intent => "Identity, purpose, tenant, and classification recorded",
        DeliveryStage.EnterpriseContext => "Authorized enterprise context loaded",
        DeliveryStage.ExistingSystems => "Authorized existing systems loaded",
        DeliveryStage.ExistingArchitecture => "Approved architecture facts authorized and evidenced",
        DeliveryStage.ApprovedPackages => "Only approved packages selected",
        DeliveryStage.AiPlanning => "AI proposes inside approved boundaries",
        DeliveryStage.SecurityValidation => "Security controls satisfied",
        DeliveryStage.Sandbox => "Change isolated from production",
        DeliveryStage.HumanReview => "Separation-of-duties approval recorded",
        DeliveryStage.Git => "Immutable source history established",
        DeliveryStage.CiCd => "Governed pipeline and attestations executed",
        DeliveryStage.Deployment => "Approved artifact deployed without AI authority",
        DeliveryStage.OpenTelemetry => "Operational telemetry correlated",
        DeliveryStage.AutomaticRegistration => "Service registered automatically",
        DeliveryStage.EnterpriseModel => "Institutional context updated",
        DeliveryStage.Evidence => "Cryptographic trust chain completed",
        _ => "Governed stage receipt and evidence required"
    };
}
