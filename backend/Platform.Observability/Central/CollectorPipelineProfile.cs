using System.Collections.Immutable;

namespace Platform.Observability.Central;

public sealed record CollectorPipelineProfile(
    Uri AgentEndpoint,
    Uri GatewayEndpoint,
    bool TraceAwareRoutingEnabled,
    bool IsLocallyOperated,
    string TrustAnchorReference,
    ImmutableArray<string> ProcessingStages,
    ImmutableArray<TelemetryStorageBinding> StorageBindings)
{
    private static readonly string[] RequiredProcessingStages =
        ["redaction", "tenant_isolation", "classification_enforcement", "batch"];

    public CollectorPipelineProfile Validate()
    {
        ValidateEndpoint(AgentEndpoint, "collector agent");
        ValidateEndpoint(GatewayEndpoint, "collector gateway");
        ArgumentException.ThrowIfNullOrWhiteSpace(TrustAnchorReference);
        if (!TraceAwareRoutingEnabled)
            throw new InvalidOperationException("Trace-aware collector routing is mandatory.");
        foreach (var stage in RequiredProcessingStages)
            if (!ProcessingStages.Contains(stage, StringComparer.Ordinal))
                throw new InvalidOperationException($"Collector processing stage '{stage}' is required.");

        foreach (var signal in Enum.GetValues<TelemetrySignalKind>())
        {
            var matches = StorageBindings.Where(binding => binding.Signal == signal).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"Signal '{signal}' requires exactly one storage binding.");
            matches[0].Validate();
            if (IsLocallyOperated && !matches[0].IsLocallyOperated)
                throw new InvalidOperationException("A locally operated collector requires locally operated storage.");
        }
        return this;
    }

    private static void ValidateEndpoint(Uri endpoint, string component)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri || !StringComparer.OrdinalIgnoreCase.Equals(endpoint.Scheme, Uri.UriSchemeHttps))
            throw new InvalidOperationException($"The {component} endpoint must be absolute HTTPS.");
    }
}
