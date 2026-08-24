namespace Platform.Identity;

public sealed class IdentityProviderOptions
{
    public const string SectionName = "IdentityProvider";

    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = true;

    public bool IsOperationallyConfigured =>
        Uri.TryCreate(Authority, UriKind.Absolute, out var authority) &&
        (!RequireHttpsMetadata || authority.Scheme == Uri.UriSchemeHttps) &&
        !string.IsNullOrWhiteSpace(Audience);
}
