namespace Platform.SoftwareFactory.InternalService;

public enum ExistingSystemsRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed record ExistingSystemsRuntimeReadiness(
    ExistingSystemsRuntimeConfigurationState State)
{
    public bool IsOperationallyConfigured =>
        State == ExistingSystemsRuntimeConfigurationState.Configured;
}
