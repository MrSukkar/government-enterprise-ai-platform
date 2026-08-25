namespace Platform.AgenticWork.Execution;

public sealed record AgentStepExecutionContext(
    Guid WorkId,
    string TenantId,
    AgenticWorkStep Step,
    string IdempotencyKey,
    string? DurableCheckpointReference,
    DateTimeOffset RequestedAt);
