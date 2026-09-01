using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;

namespace Platform.Integrations.ExistingArchitecture;

public enum ExistingArchitectureItemKind
{
    Boundary,
    Component,
    Module,
    Dependency,
    Interface,
    Constraint,
    DecisionReference,
    TechnologyBaselineReference
}

public enum ExistingArchitectureApprovalState
{
    Approved,
    Draft,
    Discovered,
    Inferred,
    Unknown,
    Superseded
}

public sealed record ExistingArchitectureSourceScope(
    string TenantId,
    string Purpose,
    string Environment,
    DataClassification MaximumClassification,
    ImmutableHashSet<EnterpriseObjectId> AllowedSystemIds,
    ImmutableHashSet<ExistingArchitectureItemKind> AllowedItemKinds,
    ImmutableHashSet<string> AllowedRelationshipTypes,
    ImmutableHashSet<string> AllowedSourceKinds,
    int MaximumResults);

public sealed record ExistingArchitectureCandidate(
    string SourceKind,
    Guid ArchitectureItemId,
    EnterpriseObjectId SystemId,
    EnterpriseObjectId? RelatedSystemId,
    ExistingArchitectureItemKind Kind,
    string Name,
    string Description,
    string? RelationshipType,
    ExistingArchitectureApprovalState ApprovalState,
    string Version,
    DataClassification Classification,
    string Environment,
    LifecycleState Lifecycle,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset ApprovedAt,
    DateTimeOffset UpdatedAt,
    bool CredentialsIncluded,
    bool LiveSessionIncluded,
    bool ExecutableCommandIncluded,
    bool GeneratedContentIncluded,
    bool ExternalEffectOccurred);

public interface IExistingArchitectureSource
{
    string SourceKind { get; }

    Task<IReadOnlyCollection<ExistingArchitectureCandidate>> DiscoverAsync(
        ExistingArchitectureSourceScope scope,
        CancellationToken cancellationToken);
}
