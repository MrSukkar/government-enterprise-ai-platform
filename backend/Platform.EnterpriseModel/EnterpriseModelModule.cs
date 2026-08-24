using Platform.Application.Abstractions;

namespace Platform.EnterpriseModel;

public sealed class EnterpriseModelModule : IPlatformModule
{
    public string Name => "EnterpriseModel";
    public System.Reflection.Assembly Assembly => typeof(EnterpriseModelModule).Assembly;
}
