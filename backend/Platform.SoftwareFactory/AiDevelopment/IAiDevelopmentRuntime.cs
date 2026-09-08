namespace Platform.SoftwareFactory.AiDevelopment;

public interface IAiDevelopmentRuntime
{
    string RuntimeProfile { get; }
    int RequestTimeoutSeconds { get; }
    int MaximumRequestBytes { get; }
    int MaximumResponseBytes { get; }

    Task<AiCandidateArtifact> ExecuteAsync(
        AiDevelopmentRequest request,
        CancellationToken cancellationToken);
}
