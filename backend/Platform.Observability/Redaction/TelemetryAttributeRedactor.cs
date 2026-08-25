namespace Platform.Observability.Redaction;

public sealed class TelemetryAttributeRedactor
{
    private readonly TelemetryRedactionPolicy _policy;

    public TelemetryAttributeRedactor(TelemetryRedactionPolicy policy)
    {
        _policy = policy.Validate();
    }

    public TelemetryRedactionResult Redact(string attributeName, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeName);

        if (_policy.SensitiveNameFragments.Any(fragment =>
                attributeName.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            return new(TelemetryAttributeDisposition.Drop, null);

        var allowed = _policy.AllowedAttributeNames.Contains(attributeName) ||
            _policy.AllowedAttributePrefixes.Any(prefix =>
                attributeName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (!allowed)
            return new(TelemetryAttributeDisposition.Redact, _policy.RedactedValue);

        if (value is string text && text.Length > _policy.MaximumStringLength)
            return new(TelemetryAttributeDisposition.Keep, text[.._policy.MaximumStringLength]);

        return new(TelemetryAttributeDisposition.Keep, value);
    }
}
