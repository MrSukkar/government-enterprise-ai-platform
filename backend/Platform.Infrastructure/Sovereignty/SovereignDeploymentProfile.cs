using System.Collections.Immutable;

namespace Platform.Infrastructure.Sovereignty;

public sealed record SovereignDeploymentProfile(
    Guid Id,
    string TenantId,
    string EnvironmentName,
    DeploymentTopology Topology,
    bool ExternalControlPlaneAllowed,
    bool ExternalApiAllowed,
    bool ExternalAiServiceAllowed,
    bool ExternalSaasAllowed,
    bool OutboundNetworkDefaultDeny,
    ImmutableArray<SovereignDependencyEndpoint> Dependencies)
{
    public SovereignDeploymentProfile Validate()
    {
        if (Id == Guid.Empty) throw new InvalidOperationException("Deployment profile identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(EnvironmentName);
        if (Dependencies.IsDefaultOrEmpty)
            throw new InvalidOperationException("Sovereign dependencies are required.");

        var required = Enum.GetValues<SovereignDependencyKind>();
        var duplicate = Dependencies.GroupBy(item => item.Kind).FirstOrDefault(group => group.Count() != 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Sovereign dependency '{duplicate.Key}' must have exactly one binding.");
        if (required.Any(kind => Dependencies.All(item => item.Kind != kind)))
            throw new InvalidOperationException("Every sovereign dependency requires a binding.");
        foreach (var dependency in Dependencies) dependency.Validate();

        if (Topology == DeploymentTopology.AirGapped &&
            (ExternalControlPlaneAllowed || ExternalApiAllowed || ExternalAiServiceAllowed || ExternalSaasAllowed ||
             !OutboundNetworkDefaultDeny || Dependencies.Any(item => !item.IsLocallyOperated)))
            throw new InvalidOperationException("Air-gapped deployment requires local dependencies, default-deny outbound networking, and no external control plane, API, AI, or SaaS dependency.");

        return this;
    }
}
