using System.Diagnostics;
using System.Diagnostics.Metrics;
using Platform.Observability.Redaction;

namespace Platform.Observability.OpenTelemetry;

public sealed class PlatformTelemetry
{
    public const string ActivitySourceName = "GovernmentEnterpriseAIPlatform";
    public const string MeterName = "GovernmentEnterpriseAIPlatform";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Operations = Meter.CreateCounter<long>("platform.operations");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("platform.operation.duration", "ms");

    private readonly TelemetryAttributeRedactor _redactor;

    public PlatformTelemetry(TelemetryAttributeRedactor redactor)
    {
        _redactor = redactor;
    }

    public Activity? StartOperation(
        string operationName,
        ActivityKind kind,
        IReadOnlyDictionary<string, object?>? attributes = null)
    {
        ValidateTelemetryToken(operationName, nameof(operationName));
        var activity = ActivitySource.StartActivity(operationName, kind);
        if (activity is null || attributes is null) return activity;

        foreach (var attribute in attributes)
            Apply(activity, attribute.Key, attribute.Value);
        return activity;
    }

    public void RecordOperation(string operationName, string outcome, TimeSpan elapsed)
    {
        ValidateTelemetryToken(operationName, nameof(operationName));
        ValidateTelemetryToken(outcome, nameof(outcome));
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));

        var tags = new TagList
        {
            { "operation.name", RedactedValue("operation.name", operationName) },
            { "operation.outcome", RedactedValue("operation.outcome", outcome) }
        };
        Operations.Add(1, tags);
        Duration.Record(elapsed.TotalMilliseconds, tags);
    }

    private void Apply(Activity activity, string name, object? value)
    {
        var result = _redactor.Redact(name, value);
        if (result.Disposition != TelemetryAttributeDisposition.Drop)
            activity.SetTag(name, result.Value);
    }

    private object? RedactedValue(string name, object? value)
    {
        var result = _redactor.Redact(name, value);
        return result.Disposition == TelemetryAttributeDisposition.Drop ? null : result.Value;
    }

    private static void ValidateTelemetryToken(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > 64 || !char.IsAsciiLetterLower(value[0]) ||
            value.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '_' and not '-'))
            throw new ArgumentException("Telemetry names must be low-cardinality lowercase tokens.", parameterName);
    }
}
