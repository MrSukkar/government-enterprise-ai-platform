namespace Platform.Web.Foundation;

public sealed record PlatformArea(string Key, string Name, string Description, string PhaseAvailability)
{
    public static IReadOnlyList<PlatformArea> Approved { get; } =
    [
        new("BUILD", "Build", "Governed software delivery and engineering lifecycle.", "Capabilities arrive in later approved phases."),
        new("UNDERSTAND", "Understand", "Enterprise context, knowledge, systems, and relationships.", "Capabilities arrive in later approved phases."),
        new("OPERATE", "Operate", "Services, operations, telemetry, and resilience.", "Capabilities arrive in later approved phases."),
        new("ACT", "Act", "Policy-governed decisions, approvals, and permitted action.", "Capabilities arrive in later approved phases.")
    ];
}
