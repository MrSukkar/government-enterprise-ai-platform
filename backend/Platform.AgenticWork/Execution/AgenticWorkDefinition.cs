using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.AgenticWork.Execution;

public sealed record AgenticWorkDefinition(
    Guid Id,
    string TenantId,
    string InitiatorSubjectId,
    string Purpose,
    DataClassification Classification,
    ImmutableArray<string> PolicyReferences,
    ImmutableArray<string> EvidenceReferences,
    ImmutableArray<AgenticWorkStep> Steps,
    DateTimeOffset CreatedAt)
{
    public AgenticWorkDefinition Validate()
    {
        if (Id == Guid.Empty) throw new InvalidOperationException("Agentic work identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(InitiatorSubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (CreatedAt == default) throw new InvalidOperationException("Agentic work creation time is required.");
        if (PolicyReferences.IsDefaultOrEmpty || EvidenceReferences.IsDefaultOrEmpty || Steps.IsDefaultOrEmpty)
            throw new InvalidOperationException("Agentic work policies, evidence, and steps are required.");
        foreach (var value in PolicyReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var value in EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var step in Steps) step.Validate();
        if (Steps.Select(step => step.Id).Distinct(StringComparer.Ordinal).Count() != Steps.Length)
            throw new InvalidOperationException("Agent step identities must be unique.");
        if (!Steps.OrderBy(step => step.Ordinal).Select(step => step.Ordinal).SequenceEqual(Enumerable.Range(0, Steps.Length)))
            throw new InvalidOperationException("Agent step ordinals must be contiguous and zero-based.");
        return this;
    }
}
