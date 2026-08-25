using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.Governance.GovernedActions;

public sealed record AuthorizedActionCommand(
    Guid RequestId,
    Guid DecisionRequestId,
    string TenantId,
    string ActorSubjectId,
    string Environment,
    DataClassification Classification,
    string ActionName,
    string TargetResource,
    ImmutableSortedDictionary<string, string> Parameters,
    string PolicyBundleId,
    string PolicyBundleVersion,
    string PolicyBundleSha256Digest,
    string ApprovalEvidenceReference,
    string IdempotencyKey,
    ImmutableArray<string> EvidenceReferences);
