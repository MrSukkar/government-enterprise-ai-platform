namespace Platform.SoftwareFactory.Sandbox;

public interface ISecuritySandboxRuntime
{
    Task<SandboxExecutionResult> ExecuteAsync(
        SandboxExecutionRequest request,
        CancellationToken cancellationToken);
}
