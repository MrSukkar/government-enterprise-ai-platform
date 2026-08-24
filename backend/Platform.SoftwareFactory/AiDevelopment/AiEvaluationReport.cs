using System.Collections.Immutable;

namespace Platform.SoftwareFactory.AiDevelopment;

public sealed record AiEvaluationReport(
    string EvaluatorId,
    bool IsIndependentFromGenerationRuntime,
    ImmutableArray<AiEvaluationFinding> Findings,
    DateTimeOffset EvaluatedAt)
{
    private static readonly ImmutableHashSet<AiEvaluationCriterion> RequiredCriteria =
        Enum.GetValues<AiEvaluationCriterion>().ToImmutableHashSet();

    public bool IsAccepted =>
        IsIndependentFromGenerationRuntime &&
        Findings.Select(finding => finding.Criterion).ToImmutableHashSet().IsSupersetOf(RequiredCriteria) &&
        Findings.All(finding => finding.Passed && !string.IsNullOrWhiteSpace(finding.EvidenceReference));

    public AiEvaluationReport Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(EvaluatorId);
        if (!IsIndependentFromGenerationRuntime)
            throw new InvalidOperationException("AI output evaluation must be independent from the generation runtime.");
        if (!Findings.Select(finding => finding.Criterion).ToImmutableHashSet().IsSupersetOf(RequiredCriteria))
            throw new InvalidOperationException("The AI evaluation report is incomplete.");
        return this;
    }
}
