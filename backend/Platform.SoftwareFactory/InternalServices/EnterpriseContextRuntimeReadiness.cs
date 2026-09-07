namespace Platform.SoftwareFactory.InternalService;

public enum EnterpriseContextRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed record EnterpriseContextRuntimeReadiness(
    EnterpriseContextRuntimeConfigurationState State)
{
    public bool IsOperationallyConfigured =>
        State == EnterpriseContextRuntimeConfigurationState.Configured;
}
