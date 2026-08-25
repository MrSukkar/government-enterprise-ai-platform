using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Understanding;

public sealed record UnderstandingClaim(
    string Statement,
    RelationshipKnowledgeState KnowledgeState,
    decimal Confidence,
    DataClassification Classification,
    ImmutableArray<string> EvidenceReferences,
    ImmutableArray<EnterpriseObjectId> EnterpriseObjectReferences)
{
    public UnderstandingClaim Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Statement);
        if (Confidence is < 0m or > 1m) throw new ArgumentOutOfRangeException(nameof(Confidence));
        if (EvidenceReferences.IsDefault || EnterpriseObjectReferences.IsDefault)
            throw new InvalidOperationException("Claim evidence and object references must be explicitly supplied.");
        if (KnowledgeState != RelationshipKnowledgeState.Unknown && EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Confirmed, discovered, and inferred claims require evidence.");
        if (KnowledgeState == RelationshipKnowledgeState.Unknown && Confidence != 0m)
            throw new InvalidOperationException("Unknown claims cannot assert confidence.");
        foreach (var evidenceReference in EvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        return this;
    }
}
