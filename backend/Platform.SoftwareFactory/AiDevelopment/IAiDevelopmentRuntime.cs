namespace Platform.SoftwareFactory.AiDevelopment;

public interface IAiDevelopmentRuntime
{
    string RuntimeProfile { get; }

    Task<AiCandidateArtifact> ExecuteAsync(
        AiDevelopmentRequest request,
        CancellationToken cancellationToken);
}
