using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.Observability.Central;

public sealed record ObservabilityRecord(
    string Id,
    TelemetrySignalKind Signal,
    string TenantId,
    string EnvironmentName,
    string ServiceName,
    string OperationName,
    string CorrelationTraceId,
    DateTimeOffset ObservedAt,
    DataClassification Classification,
    ImmutableDictionary<string, object?> RedactedAttributes,
    ImmutableArray<string> EnterpriseObjectReferences);
