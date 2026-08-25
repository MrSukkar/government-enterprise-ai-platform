using System.Collections.Immutable;
using Platform.EnterpriseModel.Model;

namespace Platform.Modeling.Simulation;

public sealed record SimulatedImpact(
    EnterpriseObjectId ObjectId,
    string Projection,
    decimal Confidence,
    ImmutableArray<string> EvidenceReferences);

public sealed record SimulationResult(
    Guid RequestId,
    Guid ScenarioId,
    Guid SnapshotId,
    string IdempotencyKey,
    ImmutableArray<SimulatedImpact> ProjectedImpacts,
    ImmutableArray<string> Assumptions,
    ImmutableArray<string> EvidenceReferences,
    string RecoveryAssessmentReference,
    DateTimeOffset CompletedAt)
{
    public bool IsExternallyEffecting => false;
    public bool IsAuthoritativeDecision => false;
}
