namespace Platform.SoftwareFactory.InternalService;

public enum ApprovedPackagesRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed record ApprovedPackagesRuntimeReadiness(ApprovedPackagesRuntimeConfigurationState State)
{
    public bool IsOperationallyConfigured => State == ApprovedPackagesRuntimeConfigurationState.Configured;
}
