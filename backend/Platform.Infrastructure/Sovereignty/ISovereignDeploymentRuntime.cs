namespace Platform.Infrastructure.Sovereignty;

public interface ISovereignDeploymentRuntime
{
    Task<SovereignDeploymentReceipt> DeployAsync(
        SovereignDeploymentRequest request,
        CancellationToken cancellationToken);
}
