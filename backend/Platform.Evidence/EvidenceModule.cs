using Platform.Application.Abstractions;

namespace Platform.Evidence;

public sealed class EvidenceModule : IPlatformModule
{
    public string Name => "Evidence";
    public System.Reflection.Assembly Assembly => typeof(EvidenceModule).Assembly;
}
