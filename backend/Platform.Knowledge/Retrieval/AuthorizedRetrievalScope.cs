using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.Knowledge.Retrieval;

public sealed record AuthorizedRetrievalScope(
    string TenantId,
    string Purpose,
    DataClassification MaximumClassification,
    ImmutableHashSet<string> AllowedResourceIds,
    ImmutableHashSet<RetrievalModality> Modalities,
    int MaximumResults);
