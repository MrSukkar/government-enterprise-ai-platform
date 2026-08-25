using System.Collections.Immutable;

namespace Platform.AgenticWork.Execution;

public sealed record AgentStepResult(
    Guid WorkId,
    string StepId,
    string IdempotencyKey,
    AgentStepOutcome Outcome,
    string DurableCheckpointReference,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CompletedAt)
{
    public bool IsExternallyEffecting => false;
}
