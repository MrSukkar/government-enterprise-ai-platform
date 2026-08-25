using System.Collections.Immutable;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Intelligence;

public sealed record ProactiveFinding(
    string FindingFingerprint,
    EnterpriseObjectId ObjectId,
    ImmutableArray<Guid> SignalIds,
    ProactiveFindingDisposition Disposition,
    string Title,
    string Rationale,
    string? RecommendedActionName,
    decimal Confidence,
    ImmutableArray<string> EvidenceReferences)
{
    public bool IsExternallyEffecting => false;
    public bool RequiresHumanReview => true;
    public bool RequiresGovernanceForAction => RecommendedActionName is not null;
}

public sealed record ProactiveIntelligenceReport(
    Guid RequestId,
    string TenantId,
    string DetectionPolicyId,
    string DetectionPolicyVersion,
    string DetectionPolicySha256Digest,
    ImmutableArray<ProactiveFinding> Findings,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd,
    DateTimeOffset GeneratedAt);
