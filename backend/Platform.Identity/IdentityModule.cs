using Platform.Application.Abstractions;

namespace Platform.Identity;

public sealed class IdentityModule : IPlatformModule
{
    public string Name => "Identity";
    public System.Reflection.Assembly Assembly => typeof(IdentityModule).Assembly;
}
