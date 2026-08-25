using System.Collections.Immutable;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Registration;

public sealed record AutomaticRegistrationRelationship(
    EnterpriseObjectId TargetId,
    string RelationshipType,
    ImmutableArray<string> EvidenceReferences)
{
    public AutomaticRegistrationRelationship Validate()
    {
        if (TargetId.Value == Guid.Empty) throw new InvalidOperationException("Relationship target is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(RelationshipType);
        if (EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Automatically registered relationships require evidence.");
        foreach (var evidenceReference in EvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        return this;
    }
}
