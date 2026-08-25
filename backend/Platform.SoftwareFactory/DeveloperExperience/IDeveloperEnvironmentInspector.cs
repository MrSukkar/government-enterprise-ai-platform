namespace Platform.SoftwareFactory.DeveloperExperience;

public interface IDeveloperEnvironmentInspector
{
    Task<DeveloperEnvironmentSnapshot> InspectAsync(
        DeveloperWorkspaceRequest request,
        CancellationToken cancellationToken);
}
