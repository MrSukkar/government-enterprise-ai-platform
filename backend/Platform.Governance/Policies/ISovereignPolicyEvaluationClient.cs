using System.Collections.Immutable;

namespace Platform.Governance.Policies;

public sealed record SovereignPolicyEvaluationRequest(
    Guid DecisionRequestId,
    string Action,
    string ResourceId,
    string TenantId,
    string SubjectId,
    string Purpose,
    string Classification,
    string Environment,
    PolicyBundleVerification VerifiedPolicyBundle,
    ImmutableSortedDictionary<string, string> Attributes,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset EvaluatedAt);

public sealed record SovereignPolicyEvaluationDecision(
    Guid DecisionRequestId,
    string Action,
    string ResourceId,
    string BundleId,
    string BundleVersion,
    string BundleSha256Digest,
    string Environment,
    OpaDecisionOutcome Outcome,
    ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences,
    SovereignPolicyEvaluationScope? Scope,
    DateTimeOffset DecidedAt);

public sealed record SovereignPolicyEvaluationScope(
    string MaximumClassification,
    ImmutableArray<string> AllowedResourceIds,
    ImmutableArray<string> AllowedModalities,
    ImmutableArray<string> RequiredRoles,
    int MaximumResults,
    SovereignExistingSystemsPolicyScope? ExistingSystems,
    SovereignExistingArchitecturePolicyScope? ExistingArchitecture,
    SovereignApprovedPackagesPolicyScope? ApprovedPackages,
    SovereignAiPlanningPolicyScope? AiPlanning,
    SovereignCodeGenerationPolicyScope? CodeGeneration,
    SovereignStaticValidationPolicyScope? StaticValidation,
    SovereignSecurityValidationPolicyScope? SecurityValidation,
    SovereignSandboxPolicyScope? Sandbox,
    SovereignTestsPolicyScope? Tests,
    SovereignHumanReviewPolicyScope? HumanReview,
    SovereignGitPolicyScope? Git);

public sealed record SovereignExistingSystemsPolicyScope(
    string MaximumClassification,
    ImmutableArray<string> AllowedSystemIds,
    ImmutableArray<string> AllowedRelationshipTypes,
    string AllowedSourceKind,
    ImmutableArray<string> RequiredRoles,
    int MaximumResults);

public sealed record SovereignExistingArchitecturePolicyScope(
    string MaximumClassification,
    ImmutableArray<string> AllowedSystemIds,
    ImmutableArray<string> AllowedItemKinds,
    ImmutableArray<string> AllowedRelationshipTypes,
    string AllowedSourceKind,
    ImmutableArray<string> RequiredRoles,
    int MaximumResults);

public sealed record SovereignApprovedPackageCoordinate(
    string Kind,
    string Name,
    string Version,
    string ContentDigest);

public sealed record SovereignApprovedPackagesPolicyScope(
    string MaximumClassification,
    ImmutableArray<SovereignApprovedPackageCoordinate> AllowedCoordinates,
    ImmutableArray<string> RequiredRoles,
    int MaximumResults);

public sealed record SovereignAiPlanningPolicyScope(
    string MaximumClassification,
    string AllowedPromptTemplateId,
    string AllowedPromptTemplateVersion,
    string AllowedPromptSha256Digest,
    string AllowedRuntimeProfile,
    ImmutableArray<string> AllowedContextReferences,
    ImmutableArray<SovereignApprovedPackageCoordinate> AllowedPackages,
    ImmutableArray<string> AllowedConstraints,
    ImmutableArray<string> RequiredRoles,
    string OutputKind,
    int RequestTimeoutSeconds,
    int MaximumRequestBytes,
    int MaximumResponseBytes);

public sealed record SovereignCodeGenerationPolicyScope(
    string MaximumClassification,
    string AllowedPromptTemplateId,
    string AllowedPromptTemplateVersion,
    string AllowedPromptSha256Digest,
    string AllowedRuntimeProfile,
    ImmutableArray<string> AllowedContextReferences,
    ImmutableArray<SovereignApprovedPackageCoordinate> AllowedPackages,
    ImmutableArray<string> AllowedConstraints,
    ImmutableArray<string> AllowedOutputPaths,
    ImmutableArray<string> RequiredRoles,
    string OutputKind,
    int RequestTimeoutSeconds,
    int MaximumRequestBytes,
    int MaximumResponseBytes);

public sealed record SovereignStaticValidationPolicyScope(
    string MaximumClassification,
    ImmutableArray<string> AllowedControlIds,
    ImmutableArray<string> RequiredRoles,
    string OutputKind);

public sealed record SovereignSecurityValidationPolicyScope(
    string MaximumClassification,
    ImmutableArray<string> AllowedControlIds,
    ImmutableArray<string> RequiredRoles,
    string OutputKind);

public sealed record SovereignSandboxPolicyScope(
    string MaximumClassification,
    SovereignApprovedPackageCoordinate? AllowedSandboxImage,
    string IsolationClass,
    bool Ephemeral,
    bool MicroVmIsolation,
    bool ProductionCredentialsAllowed,
    bool HostFilesystemAccessAllowed,
    bool NetworkDefaultDeny,
    int CpuLimit,
    long MemoryLimitBytes,
    long ExecutionTimeoutTicks,
    ImmutableSortedDictionary<string, string> AllowedEnvironmentReferences,
    ImmutableArray<string> AllowedNetworkDestinations,
    ImmutableArray<string> RequiredRoles,
    string OutputKind);

public sealed record SovereignTestsPolicyScope(
    string MaximumClassification,
    string TestManifestReference,
    string TestManifestSha256Digest,
    ImmutableArray<string> RequiredTestIds,
    ImmutableArray<string> AllowedTestCategories,
    SovereignApprovedPackageCoordinate? AllowedTestImage,
    string IsolationClass,
    bool Ephemeral,
    bool MicroVmIsolation,
    bool ProductionCredentialsAllowed,
    bool HostFilesystemAccessAllowed,
    bool NetworkDefaultDeny,
    int CpuLimit,
    long MemoryLimitBytes,
    long ExecutionTimeoutTicks,
    ImmutableSortedDictionary<string, string> AllowedEnvironmentReferences,
    ImmutableArray<string> AllowedNetworkDestinations,
    ImmutableArray<string> RequiredRoles,
    string OutputKind);

public sealed record SovereignHumanReviewPolicyScope(
    string MaximumClassification,
    string ReviewerSubjectId,
    string InitiatorSubjectId,
    string HumanDecision,
    string RationaleSha256Digest,
    string HumanAttestationReference,
    ImmutableArray<string> ConflictingSubjectIds,
    bool ReviewerIsHuman,
    long ExpectedVersion,
    ImmutableArray<string> RequiredRoles,
    string OutputKind);

public sealed record SovereignGitPolicyScope(
    string MaximumClassification,
    ImmutableArray<string> AllowedRelativePaths,
    string RepositoryId,
    string ExpectedBaseCommitId,
    string ChangeBranch,
    string CommitMessage,
    string SigningPolicyReference,
    bool ProtectedBranch,
    bool ForceUpdateAllowed,
    bool CiCdTriggerAllowed,
    ImmutableArray<string> RequiredRoles,
    string OutputKind);

public interface ISovereignPolicyEvaluationClient
{
    Task<SovereignPolicyEvaluationDecision> EvaluateAsync(
        SovereignPolicyEvaluationRequest request,
        CancellationToken cancellationToken);
}
