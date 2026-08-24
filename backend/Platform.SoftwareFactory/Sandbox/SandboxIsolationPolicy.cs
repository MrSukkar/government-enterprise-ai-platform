using System.Collections.Immutable;

namespace Platform.SoftwareFactory.Sandbox;

public sealed record SandboxIsolationPolicy(
    string IsolationClass,
    bool Ephemeral,
    bool MicroVmIsolation,
    bool ProductionCredentialsAllowed,
    bool HostFilesystemAccessAllowed,
    bool NetworkDefaultDeny,
    ImmutableHashSet<string> AllowedNetworkDestinations,
    int CpuLimit,
    long MemoryLimitBytes,
    TimeSpan ExecutionTimeout)
{
    public SandboxIsolationPolicy Validate()
    {
        if (!StringComparer.Ordinal.Equals(IsolationClass, "Firecracker-class"))
            throw new InvalidOperationException("General .NET execution requires Firecracker-class isolation.");
        if (!Ephemeral || !MicroVmIsolation)
            throw new InvalidOperationException("The sandbox must be ephemeral microVM isolation.");
        if (ProductionCredentialsAllowed || HostFilesystemAccessAllowed || !NetworkDefaultDeny)
            throw new InvalidOperationException("The sandbox policy violates mandatory isolation controls.");
        if (CpuLimit <= 0 || MemoryLimitBytes <= 0 || ExecutionTimeout <= TimeSpan.Zero)
            throw new InvalidOperationException("CPU, memory, and execution timeout limits must be configured.");
        return this;
    }
}
