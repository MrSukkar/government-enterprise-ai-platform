using Platform.Application.Abstractions;

namespace Platform.Governance;

public sealed class GovernanceModule : IPlatformModule
{
    public string Name => "Governance";
    public System.Reflection.Assembly Assembly => typeof(GovernanceModule).Assembly;
}
