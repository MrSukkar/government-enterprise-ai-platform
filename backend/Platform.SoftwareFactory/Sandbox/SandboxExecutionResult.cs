using System.Collections.Immutable;

namespace Platform.SoftwareFactory.Sandbox;

public sealed record SandboxExecutionResult(
    int ExitCode,
    bool TimedOut,
    bool IsolationViolationDetected,
    ImmutableArray<string> ProducedArtifactReferences,
    string EvidenceReference)
{
    public bool IsAccepted =>
        ExitCode == 0 &&
        !TimedOut &&
        !IsolationViolationDetected &&
        !string.IsNullOrWhiteSpace(EvidenceReference);
}
