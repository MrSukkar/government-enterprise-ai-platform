using System.Collections.Immutable;

namespace Platform.EnterpriseModel.Model;

public sealed record EnterpriseRelationship(
    EnterpriseObjectId TargetId,
    string RelationshipType,
    RelationshipKnowledgeState KnowledgeState,
    decimal Confidence,
    string Source,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset ObservedAt)
{
    public EnterpriseRelationship Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(RelationshipType);
        ArgumentException.ThrowIfNullOrWhiteSpace(Source);
        if (Confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Confidence), "Confidence must be between 0 and 1.");
        }

        if (KnowledgeState == RelationshipKnowledgeState.Confirmed && EvidenceReferences.IsDefaultOrEmpty)
        {
            throw new InvalidOperationException("A confirmed relationship requires evidence.");
        }

        return this;
    }
}
