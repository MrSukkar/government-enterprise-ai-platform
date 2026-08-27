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
        "Operational Increment 02 - Governed Intent Submission",
        "Authenticated server validation available; persistence, policy, and execution fail-closed",
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
