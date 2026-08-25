using Microsoft.Extensions.Configuration;

namespace Platform.Observability.Collection;

public sealed record CollectorAgentExportProfile(
    bool Enabled,
    Uri? Endpoint,
    string? TrustAnchorReference)
{
    public static CollectorAgentExportProfile FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection("Observability:CollectorAgent");
        var enabled = section.GetValue<bool>("Enabled");
        var endpointText = section["Endpoint"];
        var endpoint = string.IsNullOrWhiteSpace(endpointText) ? null : new Uri(endpointText, UriKind.Absolute);
        return new CollectorAgentExportProfile(
            enabled,
            endpoint,
            section["TrustAnchorReference"]).Validate();
    }

    public CollectorAgentExportProfile Validate()
    {
        if (!Enabled) return this;
        ArgumentNullException.ThrowIfNull(Endpoint);
        if (!Endpoint.IsAbsoluteUri || !StringComparer.OrdinalIgnoreCase.Equals(Endpoint.Scheme, Uri.UriSchemeHttps))
            throw new InvalidOperationException("The collector agent endpoint must be absolute HTTPS.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TrustAnchorReference);
        return this;
    }
}
