using System.Collections.Immutable;

namespace Platform.SoftwareFactory.DeveloperExperience;

public sealed record DeveloperWorkspaceRequest(
    Guid RequestId,
    string TenantId,
    string DeveloperSubjectId,
    ImmutableHashSet<string> Permissions,
    string Purpose,
    string WorkspaceRelativePath,
    string GitRepositoryReference,
    string GitBranch,
    string LocalPackageSourceReference,
    ApprovedDeveloperTemplate Template,
    DateTimeOffset RequestedAt)
{
    public DeveloperWorkspaceRequest Validate()
    {
        if (RequestId == Guid.Empty) throw new InvalidOperationException("Developer workspace request identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(DeveloperSubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(WorkspaceRelativePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(GitRepositoryReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(GitBranch);
        ArgumentException.ThrowIfNullOrWhiteSpace(LocalPackageSourceReference);
        ArgumentNullException.ThrowIfNull(Permissions);
        ArgumentNullException.ThrowIfNull(Template);
        if (!Permissions.Contains("developer.workspace.bootstrap"))
            throw new UnauthorizedAccessException("The developer.workspace.bootstrap permission is required.");
        if (Path.IsPathRooted(WorkspaceRelativePath) || WorkspaceRelativePath.Split('/', '\\').Contains(".."))
            throw new InvalidOperationException("Developer workspace path must be relative and cannot traverse parents.");
        Template.Validate();
        if (RequestedAt == default) throw new InvalidOperationException("Developer workspace request time is required.");
        return this;
    }
}
