using System.Collections.Immutable;

namespace Platform.Evidence.Chain;

public sealed record EvidenceVerificationRequest(
    Guid ChainId,
    string TenantId,
    string RequestingSubjectId,
    ImmutableHashSet<string> Permissions,
    string Purpose,
    string MaximumAuthorizedClassification,
    DateTimeOffset RequestedAt)
{
    public EvidenceVerificationRequest Validate()
    {
        if (ChainId == Guid.Empty) throw new InvalidOperationException("Evidence chain identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(RequestingSubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(MaximumAuthorizedClassification);
        ArgumentNullException.ThrowIfNull(Permissions);
        if (!Permissions.Contains("evidence.verify"))
            throw new UnauthorizedAccessException("The evidence.verify permission is required.");
        if (RequestedAt == default) throw new InvalidOperationException("Evidence verification time is required.");
        return this;
    }
}
