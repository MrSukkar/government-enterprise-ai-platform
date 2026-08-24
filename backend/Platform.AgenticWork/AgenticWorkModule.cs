using Platform.Application.Abstractions;

namespace Platform.AgenticWork;

public sealed class AgenticWorkModule : IPlatformModule
{
    public string Name => "AgenticWork";
    public System.Reflection.Assembly Assembly => typeof(AgenticWorkModule).Assembly;
}
