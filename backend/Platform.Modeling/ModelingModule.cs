using Platform.Application.Abstractions;

namespace Platform.Modeling;

public sealed class ModelingModule : IPlatformModule
{
    public string Name => "Modeling";
    public System.Reflection.Assembly Assembly => typeof(ModelingModule).Assembly;
}
