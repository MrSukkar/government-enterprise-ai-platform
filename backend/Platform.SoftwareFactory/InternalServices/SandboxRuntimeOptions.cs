namespace Platform.SoftwareFactory.InternalService;

public enum SandboxRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed class SandboxRuntimeOptions
{
    public const string SectionName = "Platform:SoftwareFactory:SandboxRuntime";
    public string Endpoint { get; init; } = string.Empty;
    public string RuntimeProfile { get; init; } = string.Empty;
    public string OperatorId { get; init; } = string.Empty;
    public string SignatureAlgorithm { get; init; } = string.Empty;
    public Dictionary<string, string> TrustedPublicKeysPem { get; init; } = new(StringComparer.Ordinal);
    public int RequestTimeoutSeconds { get; init; }
    public int MaximumRequestBytes { get; init; }
    public int MaximumResponseBytes { get; init; }

    public SandboxRuntimeConfigurationState ConfigurationState
    {
        get
        {
            if (new[] { Endpoint, RuntimeProfile, OperatorId, SignatureAlgorithm }.All(string.IsNullOrWhiteSpace) &&
                TrustedPublicKeysPem.Count == 0 && RequestTimeoutSeconds == 0 &&
                MaximumRequestBytes == 0 && MaximumResponseBytes == 0)
                return SandboxRuntimeConfigurationState.Unconfigured;
            if (!ValidEndpoint(Endpoint) || string.IsNullOrWhiteSpace(RuntimeProfile) ||
                string.IsNullOrWhiteSpace(OperatorId) || SignatureAlgorithm is not ("RS256" or "ES256") ||
                TrustedPublicKeysPem.Count == 0 || RequestTimeoutSeconds <= 0 || MaximumRequestBytes <= 0 ||
                MaximumResponseBytes <= 0 || TrustedPublicKeysPem.Any(pair => string.IsNullOrWhiteSpace(pair.Key) ||
                    string.IsNullOrWhiteSpace(pair.Value) || !pair.Value.Contains("PUBLIC KEY", StringComparison.Ordinal)))
                return SandboxRuntimeConfigurationState.Invalid;
            return SandboxRuntimeConfigurationState.Configured;
        }
    }

    public bool IsOperationallyConfigured => ConfigurationState == SandboxRuntimeConfigurationState.Configured;

    private static bool ValidEndpoint(string value) => Uri.TryCreate(value, UriKind.Absolute, out var endpoint) &&
        StringComparer.OrdinalIgnoreCase.Equals(endpoint.Scheme, Uri.UriSchemeHttps) &&
        string.IsNullOrEmpty(endpoint.UserInfo) && string.IsNullOrEmpty(endpoint.Query) &&
        string.IsNullOrEmpty(endpoint.Fragment);
}

public sealed record SandboxRuntimeReadiness(SandboxRuntimeConfigurationState State)
{
    public bool IsOperationallyConfigured => State == SandboxRuntimeConfigurationState.Configured;
}
