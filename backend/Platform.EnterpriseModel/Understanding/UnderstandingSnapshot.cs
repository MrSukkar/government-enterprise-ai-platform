using System.Collections.Immutable;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Understanding;

public sealed record UnderstandingSnapshot(
    Guid RequestId,
    string TenantId,
    ImmutableArray<EnterpriseObject> EnterpriseObjects,
    ImmutableArray<UnderstandingFact> Facts,
    ImmutableHashSet<string> AuthorizedEvidenceReferences,
    DateTimeOffset CapturedAt);
