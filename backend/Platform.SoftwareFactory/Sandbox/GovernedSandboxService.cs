using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.Sandbox;

public sealed class GovernedSandboxService(ISecuritySandboxRuntime runtime)
{
    public async Task<SandboxExecutionResult> ExecuteAsync(
        SandboxExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Run.Validate();
        request.Candidate.Validate();
        request.SandboxImage.Validate();
        request.IsolationPolicy.Validate();

        if (request.Run.CurrentStage != DeliveryStage.SecurityValidation)
            throw new InvalidOperationException("Sandbox execution requires a passed Security Validation stage.");
        if (request.SandboxImage.Kind != PackageKind.SandboxImage)
            throw new InvalidOperationException("The execution image must be an institutional sandbox image.");
        if (!request.SandboxImageDecision.IsAllowed)
            throw new InvalidOperationException("The sandbox image is not institutionally approved.");
        if (request.NonSecretEnvironmentReferences.Keys.Any(key =>
            key.Contains("SECRET", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("TOKEN", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("KEY", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Secret-like environment material is prohibited in sandbox requests.");

        var result = await runtime.ExecuteAsync(request, cancellationToken);
        if (string.IsNullOrWhiteSpace(result.EvidenceReference))
            throw new InvalidOperationException("Sandbox execution must produce an evidence reference.");
        return result;
    }
}
