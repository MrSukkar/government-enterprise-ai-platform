namespace Platform.SoftwareFactory.DeveloperExperience;

public interface IDeveloperWorkspacePlanRegistry
{
    Task<DeveloperWorkspacePlan> RegisterAtomicallyAsync(
        DeveloperWorkspacePlan plan,
        CancellationToken cancellationToken);
}
