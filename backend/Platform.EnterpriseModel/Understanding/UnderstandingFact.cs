using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Understanding;

public sealed record UnderstandingFact(
    string Id,
    string Statement,
    RelationshipKnowledgeState KnowledgeState,
    decimal Confidence,
    DataClassification Classification,
    string Source,
    ImmutableArray<string> EvidenceReferences,
    ImmutableArray<EnterpriseObjectId> EnterpriseObjectReferences,
    DateTimeOffset ObservedAt)
{
    public UnderstandingFact Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(Statement);
        ArgumentException.ThrowIfNullOrWhiteSpace(Source);
        if (Confidence is < 0m or > 1m) throw new ArgumentOutOfRangeException(nameof(Confidence));
        if (ObservedAt == default) throw new InvalidOperationException("Fact observation time is required.");
        if (EvidenceReferences.IsDefault || EnterpriseObjectReferences.IsDefault)
            throw new InvalidOperationException("Fact evidence and object references must be explicitly supplied.");
        if (KnowledgeState != RelationshipKnowledgeState.Unknown && EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Confirmed, discovered, and inferred facts require evidence.");
        if (KnowledgeState == RelationshipKnowledgeState.Unknown && Confidence != 0m)
            throw new InvalidOperationException("Unknown facts cannot assert confidence.");
        foreach (var evidenceReference in EvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        return this;
    }
}
