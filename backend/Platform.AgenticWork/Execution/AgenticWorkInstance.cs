using System.Collections.Immutable;

namespace Platform.AgenticWork.Execution;

public sealed record AgenticWorkInstance(
    AgenticWorkDefinition Definition,
    AgenticWorkState State,
    long Version,
    int NextStepOrdinal,
    string? HumanApprovalReference,
    string? DurableCheckpointReference,
    ImmutableArray<string> OutputEvidenceReferences,
    DateTimeOffset UpdatedAt)
{
    public AgenticWorkInstance Validate()
    {
        ArgumentNullException.ThrowIfNull(Definition);
        Definition.Validate();
        if (Version < 0) throw new InvalidOperationException("Agentic work version cannot be negative.");
        if (NextStepOrdinal < 0 || NextStepOrdinal > Definition.Steps.Length)
            throw new InvalidOperationException("Agentic work step cursor is invalid.");
        if (State == AgenticWorkState.Completed && NextStepOrdinal != Definition.Steps.Length)
            throw new InvalidOperationException("Completed agentic work must be at the end of its durable plan.");
        if (State is AgenticWorkState.Ready or AgenticWorkState.Running or AgenticWorkState.Suspended &&
            NextStepOrdinal >= Definition.Steps.Length)
            throw new InvalidOperationException("Active agentic work requires a remaining durable step.");
        if (OutputEvidenceReferences.IsDefault)
            throw new InvalidOperationException("Agentic work output evidence must be explicit.");
        if (UpdatedAt < Definition.CreatedAt)
            throw new InvalidOperationException("Agentic work update cannot precede creation.");
        if (State != AgenticWorkState.AwaitingApproval)
            ArgumentException.ThrowIfNullOrWhiteSpace(HumanApprovalReference);
        return this;
    }
}
