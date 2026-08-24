using Platform.Application.Abstractions;

namespace Platform.Infrastructure;

public sealed class InfrastructureModule : IPlatformModule
{
    public string Name => "Infrastructure";
    public System.Reflection.Assembly Assembly => typeof(InfrastructureModule).Assembly;
}
