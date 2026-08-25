using System.Collections.Immutable;

namespace Platform.Governance.Policies;

public enum OpaDecisionOutcome { Deny = 0, Permit = 1 }

public sealed record OpaPolicyDecision(
    Guid DecisionRequestId,
    string BundleId,
    string BundleVersion,
    string BundleSha256Digest,
    string Environment,
    OpaDecisionOutcome Outcome,
    ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);
