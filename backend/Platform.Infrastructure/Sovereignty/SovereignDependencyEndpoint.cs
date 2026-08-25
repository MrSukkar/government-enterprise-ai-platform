namespace Platform.Infrastructure.Sovereignty;

public sealed record SovereignDependencyEndpoint(
    SovereignDependencyKind Kind,
    Uri Endpoint,
    bool IsLocallyOperated,
    string TrustAnchorReference)
{
    public SovereignDependencyEndpoint Validate()
    {
        ArgumentNullException.ThrowIfNull(Endpoint);
        if (!Endpoint.IsAbsoluteUri)
            throw new InvalidOperationException($"The {Kind} endpoint must be absolute.");
        if (!StringComparer.OrdinalIgnoreCase.Equals(Endpoint.Scheme, Uri.UriSchemeHttps))
            throw new InvalidOperationException($"The {Kind} endpoint must use HTTPS.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TrustAnchorReference);
        return this;
    }
}
