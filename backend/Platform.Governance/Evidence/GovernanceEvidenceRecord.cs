using System.Collections.Immutable;

namespace Platform.Governance.Evidence;

public enum GovernanceEvidenceStage
{
    Request = 0, PolicyVerification = 1, PolicyDecision = 2, Approval = 3,
    ActionIntent = 4, Result = 5, Denial = 6
}

public sealed record GovernanceEvidenceRecord(
    Guid RequestId,
    string TenantId,
    GovernanceEvidenceStage Stage,
    string ActorSubjectId,
    string Detail,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset OccurredAt);
