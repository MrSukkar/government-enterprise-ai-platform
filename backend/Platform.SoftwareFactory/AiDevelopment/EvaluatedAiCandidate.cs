namespace Platform.SoftwareFactory.AiDevelopment;

public sealed record EvaluatedAiCandidate(
    AiCandidateArtifact Candidate,
    AiEvaluationReport Evaluation)
{
    public bool IsEligibleForWorkflowEvidence => Evaluation.IsAccepted;
    public bool IsExecutable => false;
}
