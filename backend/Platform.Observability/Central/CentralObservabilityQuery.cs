using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.Observability.Central;

public sealed record CentralObservabilityQuery(
    Guid Id,
    string SubjectId,
    ImmutableHashSet<string> Permissions,
    string TenantId,
    string EnvironmentName,
    DateTimeOffset From,
    DateTimeOffset To,
    ImmutableHashSet<TelemetrySignalKind> Signals,
    DataClassification MaximumClassification,
    string Purpose)
{
    public CentralObservabilityQuery Validate()
    {
        if (Id == Guid.Empty) throw new InvalidOperationException("Observability query identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(EnvironmentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Permissions.Contains("observability.read"))
            throw new UnauthorizedAccessException("The observability.read permission is required.");
        if (From == default || To == default || From >= To)
            throw new InvalidOperationException("A valid observability time range is required.");
        if (Signals.IsEmpty) throw new InvalidOperationException("At least one telemetry signal is required.");
        return this;
    }
}
