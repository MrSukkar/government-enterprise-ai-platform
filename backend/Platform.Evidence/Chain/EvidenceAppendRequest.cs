using System.Collections.Immutable;

namespace Platform.Evidence.Chain;

public sealed record EvidenceAppendRequest(
    Guid ChainId,
    string TenantId,
    string CorrelationId,
    EvidenceStage Stage,
    string ActorSubjectId,
    ImmutableHashSet<string> Permissions,
    string Classification,
    string Purpose,
    string PayloadSha256Digest,
    ImmutableArray<string> TraceReferences,
    DateTimeOffset OccurredAt)
{
    public EvidenceAppendRequest Validate()
    {
        if (ChainId == Guid.Empty) throw new InvalidOperationException("Evidence chain identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(CorrelationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ActorSubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Classification);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentNullException.ThrowIfNull(Permissions);
        if (!Permissions.Contains("evidence.append"))
            throw new UnauthorizedAccessException("The evidence.append permission is required.");
        EvidenceValidation.RequireSha256(PayloadSha256Digest, nameof(PayloadSha256Digest));
        EvidenceValidation.RequireReferences(TraceReferences, nameof(TraceReferences));
        if (OccurredAt == default) throw new InvalidOperationException("Evidence occurrence time is required.");
        return this;
    }
}
