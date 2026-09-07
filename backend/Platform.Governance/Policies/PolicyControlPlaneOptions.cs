namespace Platform.Governance.Policies;

public sealed class PolicyControlPlaneOptions
{
    public const string SectionName = "PolicyControlPlane";
    public string OpaEndpoint { get; init; } = string.Empty;
    public string BundleVerificationEndpoint { get; init; } = string.Empty;
    public string TrustAnchorReference { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public int RequestTimeoutSeconds { get; init; } = 0;
    public int MaximumResponseBytes { get; init; } = 0;

    public PolicyControlPlaneConfigurationState ConfigurationState
    {
        get
        {
            if (string.IsNullOrWhiteSpace(OpaEndpoint) && string.IsNullOrWhiteSpace(BundleVerificationEndpoint) &&
                string.IsNullOrWhiteSpace(TrustAnchorReference) && string.IsNullOrWhiteSpace(Environment) &&
                RequestTimeoutSeconds == 0 && MaximumResponseBytes == 0)
                return PolicyControlPlaneConfigurationState.Unconfigured;
            if (!ValidEndpoint(OpaEndpoint) || !ValidEndpoint(BundleVerificationEndpoint) ||
                string.IsNullOrWhiteSpace(TrustAnchorReference) || string.IsNullOrWhiteSpace(Environment) ||
                RequestTimeoutSeconds <= 0 || MaximumResponseBytes <= 0)
                return PolicyControlPlaneConfigurationState.Invalid;
            return PolicyControlPlaneConfigurationState.Configured;
        }
    }

    public bool IsOperationallyConfigured => ConfigurationState == PolicyControlPlaneConfigurationState.Configured;

    private static bool ValidEndpoint(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var endpoint) &&
        StringComparer.OrdinalIgnoreCase.Equals(endpoint.Scheme, Uri.UriSchemeHttps) &&
        string.IsNullOrEmpty(endpoint.UserInfo) && string.IsNullOrEmpty(endpoint.Query) &&
        string.IsNullOrEmpty(endpoint.Fragment);
}

public enum PolicyControlPlaneConfigurationState { Unconfigured, Invalid, Configured }

public sealed record PolicyControlPlaneReadiness(PolicyControlPlaneConfigurationState State)
{
    public bool IsOperationallyConfigured => State == PolicyControlPlaneConfigurationState.Configured;
}
