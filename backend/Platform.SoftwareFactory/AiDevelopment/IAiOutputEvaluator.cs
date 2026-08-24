namespace Platform.SoftwareFactory.AiDevelopment;

public interface IAiOutputEvaluator
{
    Task<AiEvaluationReport> EvaluateAsync(
        AiDevelopmentRequest request,
        AiCandidateArtifact candidate,
        CancellationToken cancellationToken);
}
