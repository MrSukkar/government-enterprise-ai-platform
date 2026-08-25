using System.Collections.Immutable;

namespace Platform.AgenticWork.Execution;

public sealed record AgenticWorkTransition(
    Guid WorkId,
    long ExpectedVersion,
    AgenticWorkState From,
    AgenticWorkState To,
    string ActorSubjectId,
    string Reason,
    string? StepId,
    string? IdempotencyKey,
    string? DurableCheckpointReference,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset OccurredAt);
