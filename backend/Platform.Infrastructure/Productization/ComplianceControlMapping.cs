using System.Collections.Immutable;

namespace Platform.Infrastructure.Productization;

public enum ComplianceControlDisposition { Implemented = 0, Inherited = 1, NotApplicable = 2 }

public sealed record ComplianceControlMapping(
    string ControlId,
    ComplianceControlDisposition Disposition,
    string ImplementationReference,
    string? NotApplicableJustification,
    ImmutableArray<string> EvidenceReferences)
{
    public ComplianceControlMapping Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ControlId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ImplementationReference);
        if (!Enum.IsDefined<ComplianceControlDisposition>(Disposition) || EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Compliance control disposition and evidence are required.");
        if (Disposition == ComplianceControlDisposition.NotApplicable)
            ArgumentException.ThrowIfNullOrWhiteSpace(NotApplicableJustification);
        else if (NotApplicableJustification is not null)
            throw new InvalidOperationException("Only not-applicable controls may carry a justification.");
        foreach (var value in EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return this;
    }
}
