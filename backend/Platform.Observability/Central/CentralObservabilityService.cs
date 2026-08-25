using System.Collections.Immutable;
using Platform.Observability.Redaction;

namespace Platform.Observability.Central;

public sealed class CentralObservabilityService(
    IObservabilityReadBackend backend,
    TelemetryAttributeRedactor redactor)
{
    public async Task<CentralObservabilityResult> QueryAsync(
        CentralObservabilityQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        query.Validate();
        var records = await backend.QueryAsync(query, cancellationToken);
        var governedRecords = records.Select(record => record with
        {
            RedactedAttributes = Redact(record.RedactedAttributes)
        }).ToArray();

        foreach (var record in governedRecords)
        {
            if (!StringComparer.Ordinal.Equals(record.TenantId, query.TenantId) ||
                !StringComparer.Ordinal.Equals(record.EnvironmentName, query.EnvironmentName) ||
                !query.Signals.Contains(record.Signal) ||
                record.ObservedAt < query.From || record.ObservedAt > query.To ||
                record.Classification > query.MaximumClassification)
                throw new UnauthorizedAccessException("Observability backend returned data outside the authorized scope.");
            ArgumentException.ThrowIfNullOrWhiteSpace(record.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(record.ServiceName);
            ArgumentException.ThrowIfNullOrWhiteSpace(record.OperationName);
            ArgumentException.ThrowIfNullOrWhiteSpace(record.CorrelationTraceId);
            foreach (var enterpriseObjectReference in record.EnterpriseObjectReferences)
                ArgumentException.ThrowIfNullOrWhiteSpace(enterpriseObjectReference);
        }

        var correlated = governedRecords
            .GroupBy(record => record.CorrelationTraceId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ObservabilityRecord>)group.OrderBy(record => record.ObservedAt).ToArray(),
                StringComparer.Ordinal);
        return new(query.Id, governedRecords, correlated, DateTimeOffset.UtcNow);
    }

    private ImmutableDictionary<string, object?> Redact(
        ImmutableDictionary<string, object?> attributes)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.Ordinal);
        foreach (var attribute in attributes)
        {
            var result = redactor.Redact(attribute.Key, attribute.Value);
            if (result.Disposition != TelemetryAttributeDisposition.Drop)
                builder[attribute.Key] = result.Value;
        }
        return builder.ToImmutable();
    }
}
