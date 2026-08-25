using System.Collections.Immutable;

namespace Platform.AgenticWork.Execution;

public sealed record AgenticWorkStep(
    string Id,
    int Ordinal,
    string Capability,
    string Purpose,
    ImmutableArray<string> InputEvidenceReferences)
{
    public AgenticWorkStep Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(Capability);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (Ordinal < 0) throw new ArgumentOutOfRangeException(nameof(Ordinal));
        if (InputEvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Agent steps require explicit input evidence.");
        foreach (var evidenceReference in InputEvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        return this;
    }
}
