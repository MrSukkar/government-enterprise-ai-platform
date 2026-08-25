namespace Platform.Infrastructure.Sovereignty;

public sealed record SovereignDeploymentRequest(
    Guid Id,
    SovereignDeploymentProfile Profile,
    VerifiedDeploymentArtifact Artifact,
    string RequestedBySubjectId,
    string Purpose,
    string HumanApprovalReference,
    DateTimeOffset RequestedAt)
{
    public SovereignDeploymentRequest Validate()
    {
        if (Id == Guid.Empty) throw new InvalidOperationException("Deployment request identity is required.");
        ArgumentNullException.ThrowIfNull(Profile);
        ArgumentNullException.ThrowIfNull(Artifact);
        Profile.Validate();
        Artifact.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(RequestedBySubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(HumanApprovalReference);
        if (RequestedAt == default) throw new InvalidOperationException("Deployment request time is required.");
        return this;
    }
}
