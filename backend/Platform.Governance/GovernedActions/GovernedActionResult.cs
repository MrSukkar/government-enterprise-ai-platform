using System.Collections.Immutable;

namespace Platform.Governance.GovernedActions;

public sealed record GovernedActionResult(
    Guid RequestId,
    string IdempotencyKey,
    bool Succeeded,
    string ResultReference,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CompletedAt);
