namespace Platform.Observability.Central;

public sealed record CentralObservabilityResult(
    Guid QueryId,
    IReadOnlyList<ObservabilityRecord> Records,
    IReadOnlyDictionary<string, IReadOnlyList<ObservabilityRecord>> CorrelatedByTrace,
    DateTimeOffset CompletedAt);
