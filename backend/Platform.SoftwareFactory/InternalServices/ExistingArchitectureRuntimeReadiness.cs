namespace Platform.SoftwareFactory.InternalService;

public enum ExistingArchitectureRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed record ExistingArchitectureRuntimeReadiness(ExistingArchitectureRuntimeConfigurationState State)
{
    public bool IsOperationallyConfigured => State == ExistingArchitectureRuntimeConfigurationState.Configured;
}
