namespace Platform.Infrastructure.Sovereignty;

public sealed class GovernedSovereignDeploymentService(ISovereignDeploymentRuntime runtime)
{
    public async Task<SovereignDeploymentReceipt> DeployAsync(
        SovereignDeploymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var receipt = await runtime.DeployAsync(request, cancellationToken);
        if (receipt.RequestId != request.Id || receipt.ProfileId != request.Profile.Id)
            throw new InvalidOperationException("Deployment runtime returned mismatched request or profile identity.");
        if (!StringComparer.Ordinal.Equals(receipt.ArtifactDigest, request.Artifact.ContentDigest))
            throw new InvalidOperationException("Deployment runtime returned a mismatched artifact digest.");
        ArgumentException.ThrowIfNullOrWhiteSpace(receipt.RuntimeIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(receipt.EvidenceReference);
        if (receipt.CompletedAt < request.RequestedAt)
            throw new InvalidOperationException("Deployment completion cannot precede the request.");
        return receipt;
    }
}
