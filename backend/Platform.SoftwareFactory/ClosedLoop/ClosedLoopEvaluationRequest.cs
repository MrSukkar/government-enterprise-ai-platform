using System.Collections.Immutable;

namespace Platform.SoftwareFactory.ClosedLoop;

public sealed record ClosedLoopEvaluationRequest(
    Guid RequestId,
    string TenantId,
    string SubjectId,
    ImmutableHashSet<string> Permissions,
    string Purpose,
    string EnterpriseObjectReference,
    string ReleaseArtifactSha256Digest,
    string ReleaseProvenanceReference,
    ClosedLoopPolicyReference Policy,
    DateTimeOffset ObservationWindowStart,
    DateTimeOffset ObservationWindowEnd,
    DateTimeOffset RequestedAt)
{
    public ClosedLoopEvaluationRequest Validate()
    {
        if (RequestId == Guid.Empty) throw new InvalidOperationException("Closed-loop request identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(EnterpriseObjectReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(ReleaseProvenanceReference);
        ArgumentNullException.ThrowIfNull(Permissions);
        ArgumentNullException.ThrowIfNull(Policy);
        if (!Permissions.Contains("software.closedloop.evaluate"))
            throw new UnauthorizedAccessException("The software.closedloop.evaluate permission is required.");
        if (ReleaseArtifactSha256Digest.Length != 64 || ReleaseArtifactSha256Digest.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Closed-loop release requires a SHA-256 digest.");
        Policy.Validate();
        if (ObservationWindowStart == default || ObservationWindowEnd <= ObservationWindowStart ||
            RequestedAt < ObservationWindowEnd)
            throw new InvalidOperationException("Closed-loop observation window is invalid.");
        return this;
    }
}
