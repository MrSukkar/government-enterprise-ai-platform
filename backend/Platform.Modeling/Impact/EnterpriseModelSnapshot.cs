using System.Collections.Immutable;
using Platform.EnterpriseModel.Model;

namespace Platform.Modeling.Impact;

public sealed record EnterpriseModelSnapshot(
    Guid RequestId,
    string TenantId,
    ImmutableArray<EnterpriseObject> Objects,
    ImmutableArray<string> AuthorizationEvidenceReferences,
    DateTimeOffset CapturedAt);
