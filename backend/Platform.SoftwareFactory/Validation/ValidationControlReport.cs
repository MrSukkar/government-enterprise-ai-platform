using System.Collections.Immutable;

namespace Platform.SoftwareFactory.Validation;

public sealed record ValidationControlReport(
    string ControlId,
    ValidationGate Gate,
    bool Completed,
    ImmutableArray<ValidationFinding> Findings,
    string EvidenceReference)
{
    public bool Passed => Completed && !Findings.Any(finding => finding.IsBlocking) && !string.IsNullOrWhiteSpace(EvidenceReference);
}
