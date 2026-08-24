using Platform.Application.Abstractions;

namespace Platform.Knowledge;

public sealed class KnowledgeModule : IPlatformModule
{
    public string Name => "Knowledge";
    public System.Reflection.Assembly Assembly => typeof(KnowledgeModule).Assembly;
}
