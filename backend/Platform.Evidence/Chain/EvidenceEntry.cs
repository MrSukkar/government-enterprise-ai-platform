using System.Collections.Immutable;

namespace Platform.Evidence.Chain;

public sealed record EvidenceEntry(
    Guid ChainId,
    string TenantId,
    string CorrelationId,
    long Sequence,
    EvidenceStage Stage,
    string ActorSubjectId,
    string Classification,
    string Purpose,
    string PayloadSha256Digest,
    string PreviousEntrySha256Digest,
    string EntrySha256Digest,
    SignatureEnvelope Signature,
    string AuthorizationEvidenceReference,
    ImmutableArray<string> TraceReferences,
    DateTimeOffset OccurredAt)
{
    public EvidenceEntry ValidateShape()
    {
        if (ChainId == Guid.Empty || Sequence < 0) throw new InvalidOperationException("Evidence identity and sequence are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(CorrelationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ActorSubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Classification);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        EvidenceValidation.RequireSha256(PayloadSha256Digest, nameof(PayloadSha256Digest));
        EvidenceValidation.RequireSha256(PreviousEntrySha256Digest, nameof(PreviousEntrySha256Digest));
        EvidenceValidation.RequireSha256(EntrySha256Digest, nameof(EntrySha256Digest));
        EvidenceValidation.RequireReferences(TraceReferences, nameof(TraceReferences));
        ArgumentNullException.ThrowIfNull(Signature);
        Signature.Validate();
        if (OccurredAt == default || Signature.SignedAt < OccurredAt)
            throw new InvalidOperationException("Evidence and signature times are invalid.");
        return this;
    }
}
