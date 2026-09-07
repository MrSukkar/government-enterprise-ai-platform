using System.Collections.Immutable;

namespace Platform.Governance.Policies;

public sealed record SovereignPolicyEvaluationRequest(
    Guid DecisionRequestId,
    string Action,
    string ResourceId,
    string TenantId,
    string SubjectId,
    string Purpose,
    string Classification,
    string Environment,
    PolicyBundleVerification VerifiedPolicyBundle,
    ImmutableSortedDictionary<string, string> Attributes,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset EvaluatedAt);

public sealed record SovereignPolicyEvaluationDecision(
    Guid DecisionRequestId,
    string Action,
    string ResourceId,
    string BundleId,
    string BundleVersion,
    string BundleSha256Digest,
    string Environment,
    OpaDecisionOutcome Outcome,
    ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface ISovereignPolicyEvaluationClient
{
    Task<SovereignPolicyEvaluationDecision> EvaluateAsync(
        SovereignPolicyEvaluationRequest request,
        CancellationToken cancellationToken);
}
