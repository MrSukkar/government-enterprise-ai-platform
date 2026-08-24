using Platform.Application.Abstractions;

namespace Platform.Integrations;

public sealed class IntegrationsModule : IPlatformModule
{
    public string Name => "Integrations";
    public System.Reflection.Assembly Assembly => typeof(IntegrationsModule).Assembly;
}
