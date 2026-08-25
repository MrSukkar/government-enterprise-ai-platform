using System.Collections.Immutable;

namespace Platform.SoftwareFactory.DeveloperExperience;

public enum DeveloperWorkflowStage
{
    MaterializeApprovedTemplate = 0,
    RestoreLockedDependencies = 1,
    VerifyProject = 2,
    Build = 3,
    Test = 4,
    ReviewChanges = 5,
    SubmitToGitAndCi = 6
}

public sealed record DeveloperWorkflowStep(
    int Ordinal,
    DeveloperWorkflowStage Stage,
    string Tool,
    ImmutableArray<string> Arguments,
    bool RequiresHumanConfirmation,
    ImmutableArray<string> EvidenceReferences);

public sealed record DeveloperWorkspacePlan(
    Guid RequestId,
    string TenantId,
    string DeveloperSubjectId,
    string WorkspaceRelativePath,
    string GitRepositoryReference,
    string GitBranch,
    string TemplateId,
    string TemplateVersion,
    string TemplateSha256Digest,
    ImmutableArray<DeveloperWorkflowStep> Steps,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CreatedAt)
{
    public bool IsProductionDeploymentCapable => false;
    public bool RequiresHumanReview => true;
}
