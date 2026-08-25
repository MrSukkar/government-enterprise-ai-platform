using System.Collections.Immutable;

namespace Platform.Modeling.Impact;

public sealed record EnterpriseImpactAnalysisReport(
    Guid RequestId,
    string TenantId,
    EnterpriseChangeProposal Change,
    ImmutableArray<EnterpriseImpact> Impacts,
    ImmutableArray<string> Limitations,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset SnapshotCapturedAt,
    DateTimeOffset GeneratedAt);
