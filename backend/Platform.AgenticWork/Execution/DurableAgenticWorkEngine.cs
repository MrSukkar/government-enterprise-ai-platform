using System.Collections.Immutable;

namespace Platform.AgenticWork.Execution;

public sealed class DurableAgenticWorkEngine(
    IDurableAgenticWorkStore store,
    IAgentRuntime runtime)
{
    public async Task<AgenticWorkInstance> StartAsync(
        AgenticWorkRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var instance = new AgenticWorkInstance(
            request.Definition,
            AgenticWorkState.AwaitingApproval,
            Version: 0,
            NextStepOrdinal: 0,
            HumanApprovalReference: null,
            DurableCheckpointReference: null,
            OutputEvidenceReferences: [],
            UpdatedAt: request.Definition.CreatedAt);
        var transition = Transition(
            instance,
            AgenticWorkState.AwaitingApproval,
            request.Definition.InitiatorSubjectId,
            "agentic_work_requested",
            evidence: request.Definition.EvidenceReferences,
            occurredAt: request.Definition.CreatedAt);
        var persisted = await store.CreateAtomicallyAsync(instance, transition, cancellationToken);
        return ValidatePersisted(instance, persisted);
    }

    public async Task<AgenticWorkInstance> ApproveAsync(
        AgenticWorkApproval approval,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(approval);
        approval.Validate();
        var current = await LoadRequiredAsync(approval.WorkId, cancellationToken);
        if (current.State != AgenticWorkState.AwaitingApproval)
            throw new InvalidOperationException("Only awaiting agentic work can be approved.");
        if (StringComparer.Ordinal.Equals(current.Definition.InitiatorSubjectId, approval.ApprovedBySubjectId))
            throw new InvalidOperationException("Agentic work approval requires separation of duties.");
        if (approval.ApprovedAt < current.UpdatedAt)
            throw new InvalidOperationException("Agentic work approval cannot precede the current state.");

        var updated = current with
        {
            State = AgenticWorkState.Ready,
            Version = current.Version + 1,
            HumanApprovalReference = approval.HumanApprovalReference,
            UpdatedAt = approval.ApprovedAt
        };
        var transition = Transition(
            current,
            AgenticWorkState.Ready,
            approval.ApprovedBySubjectId,
            "human_approval_recorded",
            evidence: [approval.HumanApprovalReference],
            occurredAt: approval.ApprovedAt);
        var persisted = await store.AppendAtomicallyAsync(updated, transition, cancellationToken);
        return ValidatePersisted(updated, persisted);
    }

    public async Task<AgenticWorkInstance> ResumeAsync(
        AgenticWorkResume resume,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resume);
        resume.Validate();
        var current = await LoadRequiredAsync(resume.WorkId, cancellationToken);
        if (current.State != AgenticWorkState.Suspended)
            throw new InvalidOperationException("Only suspended agentic work can be resumed.");
        if (resume.ResumedAt < current.UpdatedAt)
            throw new InvalidOperationException("Agentic work resume cannot precede the suspended state.");

        var updated = current with
        {
            State = AgenticWorkState.Ready,
            Version = current.Version + 1,
            UpdatedAt = resume.ResumedAt
        };
        var transition = Transition(
            current,
            AgenticWorkState.Ready,
            resume.ResumedBySubjectId,
            "operator_review_resumed",
            checkpoint: current.DurableCheckpointReference,
            evidence: [resume.ReviewEvidenceReference],
            occurredAt: resume.ResumedAt);
        var persisted = await store.AppendAtomicallyAsync(updated, transition, cancellationToken);
        return ValidatePersisted(updated, persisted);
    }

    public async Task<AgenticWorkInstance> ExecuteNextAsync(
        Guid workId,
        string workerIdentity,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerIdentity);
        var current = await LoadRequiredAsync(workId, cancellationToken);
        if (current.State is not AgenticWorkState.Ready and not AgenticWorkState.Running)
            throw new InvalidOperationException("Agentic work is not ready or resumable.");
        if (current.NextStepOrdinal >= current.Definition.Steps.Length)
            throw new InvalidOperationException("Agentic work has no remaining step.");

        var running = current;
        var step = current.Definition.Steps.Single(item => item.Ordinal == current.NextStepOrdinal);
        var idempotencyKey = $"{workId:D}:{step.Id}:{current.NextStepOrdinal}";
        if (current.State == AgenticWorkState.Ready)
        {
            running = current with
            {
                State = AgenticWorkState.Running,
                Version = current.Version + 1,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var persistedRunning = await store.AppendAtomicallyAsync(
                running,
                Transition(current, AgenticWorkState.Running, workerIdentity, "step_started", step.Id,
                    idempotencyKey, current.DurableCheckpointReference, step.InputEvidenceReferences, running.UpdatedAt),
                cancellationToken);
            running = ValidatePersisted(running, persistedRunning);
        }

        var context = new AgentStepExecutionContext(
            workId,
            running.Definition.TenantId,
            step,
            idempotencyKey,
            running.DurableCheckpointReference,
            DateTimeOffset.UtcNow);
        var result = await runtime.ExecuteStepAsync(context, cancellationToken);
        ValidateResult(running, step, idempotencyKey, result);

        var nextOrdinal = result.Outcome == AgentStepOutcome.Succeeded
            ? running.NextStepOrdinal + 1
            : running.NextStepOrdinal;
        var nextState = result.Outcome switch
        {
            AgentStepOutcome.Succeeded when nextOrdinal == running.Definition.Steps.Length => AgenticWorkState.Completed,
            AgentStepOutcome.Succeeded => AgenticWorkState.Ready,
            AgentStepOutcome.Suspended => AgenticWorkState.Suspended,
            AgentStepOutcome.Failed => AgenticWorkState.Failed,
            _ => throw new InvalidOperationException("Unsupported agent step outcome.")
        };
        var evidence = running.OutputEvidenceReferences
            .AddRange(result.EvidenceReferences)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var updated = running with
        {
            State = nextState,
            Version = running.Version + 1,
            NextStepOrdinal = nextOrdinal,
            DurableCheckpointReference = result.DurableCheckpointReference,
            OutputEvidenceReferences = evidence,
            UpdatedAt = result.CompletedAt
        };
        var persisted = await store.AppendAtomicallyAsync(
            updated,
            Transition(running, nextState, workerIdentity, $"step_{result.Outcome.ToString().ToLowerInvariant()}",
                step.Id, idempotencyKey, result.DurableCheckpointReference, result.EvidenceReferences, result.CompletedAt),
            cancellationToken);
        return ValidatePersisted(updated, persisted);
    }

    private async Task<AgenticWorkInstance> LoadRequiredAsync(Guid workId, CancellationToken cancellationToken)
    {
        if (workId == Guid.Empty) throw new ArgumentException("Agentic work identity is required.", nameof(workId));
        var instance = await store.LoadAsync(workId, cancellationToken)
            ?? throw new KeyNotFoundException("Agentic work was not found.");
        return instance.Validate();
    }

    private static void ValidateResult(
        AgenticWorkInstance running,
        AgenticWorkStep step,
        string idempotencyKey,
        AgentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.WorkId != running.Definition.Id ||
            !StringComparer.Ordinal.Equals(result.StepId, step.Id) ||
            !StringComparer.Ordinal.Equals(result.IdempotencyKey, idempotencyKey) || result.IsExternallyEffecting)
            throw new InvalidOperationException("Agent runtime returned a mismatched or externally effecting result.");
        if (!Enum.IsDefined<AgentStepOutcome>(result.Outcome))
            throw new InvalidOperationException("Agent runtime returned an invalid outcome.");
        ArgumentException.ThrowIfNullOrWhiteSpace(result.DurableCheckpointReference);
        if (result.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Agent step results require evidence.");
        foreach (var evidenceReference in result.EvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        if (result.CompletedAt < running.UpdatedAt)
            throw new InvalidOperationException("Agent step completion cannot precede durable running state.");
    }

    private static AgenticWorkInstance ValidatePersisted(
        AgenticWorkInstance expected,
        AgenticWorkInstance persisted)
    {
        ArgumentNullException.ThrowIfNull(persisted);
        persisted.Validate();
        if (persisted.Definition.Id != expected.Definition.Id ||
            !StringComparer.Ordinal.Equals(persisted.Definition.TenantId, expected.Definition.TenantId) ||
            !StringComparer.Ordinal.Equals(persisted.Definition.InitiatorSubjectId, expected.Definition.InitiatorSubjectId) ||
            !StringComparer.Ordinal.Equals(persisted.Definition.Purpose, expected.Definition.Purpose) ||
            persisted.Definition.Classification != expected.Definition.Classification ||
            persisted.Definition.CreatedAt != expected.Definition.CreatedAt ||
            !persisted.Definition.PolicyReferences.SequenceEqual(expected.Definition.PolicyReferences, StringComparer.Ordinal) ||
            !persisted.Definition.EvidenceReferences.SequenceEqual(expected.Definition.EvidenceReferences, StringComparer.Ordinal) ||
            !persisted.Definition.Steps.SequenceEqual(expected.Definition.Steps) ||
            persisted.State != expected.State || persisted.Version != expected.Version ||
            persisted.NextStepOrdinal != expected.NextStepOrdinal ||
            !StringComparer.Ordinal.Equals(persisted.HumanApprovalReference, expected.HumanApprovalReference) ||
            !StringComparer.Ordinal.Equals(persisted.DurableCheckpointReference, expected.DurableCheckpointReference) ||
            !persisted.OutputEvidenceReferences.ToHashSet(StringComparer.Ordinal)
                .SetEquals(expected.OutputEvidenceReferences) ||
            persisted.UpdatedAt != expected.UpdatedAt)
            throw new InvalidOperationException("Durable agentic work store changed governed state.");
        return persisted;
    }

    private static AgenticWorkTransition Transition(
        AgenticWorkInstance current,
        AgenticWorkState to,
        string actor,
        string reason,
        string? stepId = null,
        string? idempotencyKey = null,
        string? checkpoint = null,
        ImmutableArray<string> evidence = default,
        DateTimeOffset occurredAt = default) =>
        new(
            current.Definition.Id,
            current.Version,
            current.State,
            to,
            actor,
            reason,
            stepId,
            idempotencyKey,
            checkpoint,
            evidence.IsDefault ? [] : evidence,
            occurredAt == default ? DateTimeOffset.UtcNow : occurredAt);
}
