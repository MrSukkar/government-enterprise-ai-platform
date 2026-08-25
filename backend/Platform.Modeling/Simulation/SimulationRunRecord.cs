using System.Collections.Immutable;

namespace Platform.Modeling.Simulation;

public enum SimulationRunState { Started = 0, Completed = 1 }

public sealed record SimulationRunRecord(
    Guid RequestId,
    Guid ScenarioId,
    Guid SnapshotId,
    string TenantId,
    string IdempotencyKey,
    SimulationRunState State,
    string RecoveryPlanReference,
    string? ResultReference,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset UpdatedAt);
