namespace Platform.Observability.Central;

public sealed record TelemetryStorageBinding(
    TelemetrySignalKind Signal,
    TelemetryStorageTechnology Technology,
    Uri Endpoint,
    bool IsLocallyOperated,
    string TrustAnchorReference)
{
    public TelemetryStorageBinding Validate()
    {
        ArgumentNullException.ThrowIfNull(Endpoint);
        if (!Endpoint.IsAbsoluteUri || !StringComparer.OrdinalIgnoreCase.Equals(Endpoint.Scheme, Uri.UriSchemeHttps))
            throw new InvalidOperationException($"The {Signal} storage endpoint must be absolute HTTPS.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TrustAnchorReference);

        var expected = Signal == TelemetrySignalKind.Metrics
            ? TelemetryStorageTechnology.Prometheus
            : TelemetryStorageTechnology.OpenSearch;
        if (Technology != expected)
            throw new InvalidOperationException($"{Signal} must use the approved {expected} storage technology.");
        return this;
    }
}
