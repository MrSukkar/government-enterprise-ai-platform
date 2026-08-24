using Platform.Application.Abstractions;

namespace Platform.SoftwareFactory;

public sealed class SoftwareFactoryModule : IPlatformModule
{
    public string Name => "SoftwareFactory";
    public System.Reflection.Assembly Assembly => typeof(SoftwareFactoryModule).Assembly;
}
