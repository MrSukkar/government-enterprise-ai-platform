using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Platform.Web.Authentication;

public sealed class PlatformApiAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public PlatformApiAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigationManager,
        IConfiguration configuration)
        : base(provider, navigationManager)
    {
        var apiBaseUrl = configuration["ApiBaseUrl"];
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri) ||
            !apiUri.IsLoopback || apiUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("The Integration Demo API base URL must be localhost HTTPS.");

        ConfigureHandler([apiUri.GetLeftPart(UriPartial.Authority)]);
    }
}
