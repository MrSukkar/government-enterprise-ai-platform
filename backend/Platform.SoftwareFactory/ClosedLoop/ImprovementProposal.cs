using System.Collections.Immutable;

namespace Platform.SoftwareFactory.ClosedLoop;

public sealed record ImprovementProposal(
    Guid ProposalId,
    string Fingerprint,
    Guid RequestId,
    string TenantId,
    string EnterpriseObjectReference,
    string ReleaseArtifactSha256Digest,
    ImprovementKind Kind,
    string Title,
    string Rationale,
    string ProposedIntent,
    decimal Confidence,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CreatedAt)
{
    public bool IsExternallyEffecting => false;
    public bool RequiresHumanReview => true;
    public bool RequiresNewSoftwareDeliveryRun => true;
}
