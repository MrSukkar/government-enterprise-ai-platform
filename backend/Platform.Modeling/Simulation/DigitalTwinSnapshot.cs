using System.Collections.Immutable;
using Platform.Modeling.Impact;

namespace Platform.Modeling.Simulation;

public sealed record DigitalTwinSnapshot(
    Guid SnapshotId,
    Guid RequestId,
    string TenantId,
    string ModelVersion,
    string ModelSha256Digest,
    EnterpriseModelSnapshot Baseline,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CreatedAt)
{
    public bool IsProductionConnected => false;
}
