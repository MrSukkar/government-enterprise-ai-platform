using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;

namespace Platform.Integrations.ExistingSystems;

public sealed record ExistingSystemInventoryScope(
    string TenantId,
    string Purpose,
    DataClassification MaximumClassification,
    ImmutableHashSet<EnterpriseObjectId> AllowedSystemIds,
    ImmutableHashSet<string> AllowedRelationshipTypes,
    ImmutableHashSet<string> AllowedSourceKinds,
    int MaximumResults);

public sealed record ExistingSystemInventoryCandidate(
    string SourceKind,
    EnterpriseObject System,
    bool CredentialsIncluded,
    bool LiveSessionIncluded,
    bool ExecutableCommandIncluded,
    bool ExternalEffectOccurred);

public interface IExistingSystemInventorySource
{
    string SourceKind { get; }

    Task<IReadOnlyCollection<ExistingSystemInventoryCandidate>> DiscoverAsync(
        ExistingSystemInventoryScope scope,
        CancellationToken cancellationToken);
}
