using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.Identity.Access;

namespace Platform.Knowledge.Retrieval;

public sealed record KnowledgeQuery(
    GovernedIdentity Identity,
    string Purpose,
    string QueryText,
    string TenantId,
    DataClassification MaximumClassification,
    ImmutableHashSet<string> AllowedResourceIds,
    ImmutableHashSet<string> RequiredRoles,
    ImmutableHashSet<RetrievalModality> Modalities,
    int MaximumResults)
{
    public KnowledgeQuery Validate()
    {
        ArgumentNullException.ThrowIfNull(Identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(QueryText);
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        if (AllowedResourceIds.IsEmpty) throw new InvalidOperationException("Authorized resource scope cannot be empty.");
        if (Modalities.IsEmpty) throw new InvalidOperationException("At least one retrieval modality is required.");
        if (MaximumResults <= 0) throw new ArgumentOutOfRangeException(nameof(MaximumResults));
        return this;
    }
}
