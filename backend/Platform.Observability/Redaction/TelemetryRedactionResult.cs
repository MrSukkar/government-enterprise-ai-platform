namespace Platform.Observability.Redaction;

public sealed record TelemetryRedactionResult(
    TelemetryAttributeDisposition Disposition,
    object? Value);
