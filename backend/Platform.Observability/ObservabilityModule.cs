using Platform.Application.Abstractions;

namespace Platform.Observability;

public sealed class ObservabilityModule : IPlatformModule
{
    public string Name => "Observability";
    public System.Reflection.Assembly Assembly => typeof(ObservabilityModule).Assembly;
}
