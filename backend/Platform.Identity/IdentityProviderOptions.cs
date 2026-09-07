namespace Platform.Identity;

public sealed class IdentityProviderOptions
{
    public const string SectionName = "IdentityProvider";

    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = true;

    public bool IsOperationallyConfigured =>
        ConfigurationState == ControlPlaneConfigurationState.Configured;

    public ControlPlaneConfigurationState ConfigurationState
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Authority) && string.IsNullOrWhiteSpace(Audience))
                return ControlPlaneConfigurationState.Unconfigured;
            if (!RequireHttpsMetadata || !Uri.TryCreate(Authority, UriKind.Absolute, out var authority) ||
                !StringComparer.OrdinalIgnoreCase.Equals(authority.Scheme, Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(authority.UserInfo) || !string.IsNullOrEmpty(authority.Query) ||
                !string.IsNullOrEmpty(authority.Fragment) || string.IsNullOrWhiteSpace(Audience) ||
                Audience.Any(char.IsWhiteSpace))
                return ControlPlaneConfigurationState.Invalid;
            return ControlPlaneConfigurationState.Configured;
        }
    }
}

public enum ControlPlaneConfigurationState { Unconfigured, Invalid, Configured }

public sealed record IdentityControlPlaneReadiness(ControlPlaneConfigurationState State)
{
    public bool IsOperationallyConfigured => State == ControlPlaneConfigurationState.Configured;
}
