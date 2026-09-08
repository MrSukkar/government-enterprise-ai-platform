namespace Platform.SoftwareFactory.Packages;

public enum ApprovedPackagesTrustConfigurationState { Unconfigured, Invalid, Configured }

public sealed class ApprovedPackagesTrustOptions
{
    public const string SectionName = "Platform:SoftwareFactory:ApprovedPackagesTrust";
    public string SignatureAlgorithm { get; init; } = string.Empty;
    public Dictionary<string, string> TrustedPublicKeysPem { get; init; } = new(StringComparer.Ordinal);

    public ApprovedPackagesTrustConfigurationState ConfigurationState
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SignatureAlgorithm) && TrustedPublicKeysPem.Count == 0)
                return ApprovedPackagesTrustConfigurationState.Unconfigured;
            if (SignatureAlgorithm is not ("RS256" or "ES256") || TrustedPublicKeysPem.Count == 0 ||
                TrustedPublicKeysPem.Any(pair => string.IsNullOrWhiteSpace(pair.Key) ||
                    string.IsNullOrWhiteSpace(pair.Value) || !pair.Value.Contains("PUBLIC KEY", StringComparison.Ordinal)))
                return ApprovedPackagesTrustConfigurationState.Invalid;
            return ApprovedPackagesTrustConfigurationState.Configured;
        }
    }

    public bool IsOperationallyConfigured => ConfigurationState == ApprovedPackagesTrustConfigurationState.Configured;
}
