namespace Platform.SoftwareFactory.AiDevelopment;

public sealed record AiEvaluationFinding(
    AiEvaluationCriterion Criterion,
    bool Passed,
    string Rationale,
    string EvidenceReference);
