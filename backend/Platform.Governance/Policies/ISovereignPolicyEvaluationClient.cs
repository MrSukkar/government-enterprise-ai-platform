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
    SovereignApprovedPackagesPolicyScope? ApprovedPackages);

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

public interface ISovereignPolicyEvaluationClient
{
    Task<SovereignPolicyEvaluationDecision> EvaluateAsync(
        SovereignPolicyEvaluationRequest request,
        CancellationToken cancellationToken);
}
