using System.Collections.Immutable;

namespace Platform.Observability.Redaction;

public sealed record TelemetryRedactionPolicy(
    ImmutableHashSet<string> AllowedAttributeNames,
    ImmutableArray<string> AllowedAttributePrefixes,
    ImmutableArray<string> SensitiveNameFragments,
    int MaximumStringLength,
    string RedactedValue)
{
    public static TelemetryRedactionPolicy Strict { get; } = new(
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "deployment.environment.name",
            "error.type",
            "http.request.method",
            "http.response.status_code",
            "http.route",
            "network.protocol.name",
            "network.protocol.version",
            "operation.name",
            "operation.outcome",
            "server.address",
            "server.port",
            "service.name",
            "service.version",
            "url.scheme"),
        ["platform."],
        [
            "authorization", "cookie", "credential", "password", "secret",
            "token", "api_key", "apikey", "private_key", "connection_string",
            "url.full", "url.query", "exception.message", "exception.stacktrace"
        ],
        MaximumStringLength: 512,
        RedactedValue: "[REDACTED]");

    public TelemetryRedactionPolicy Validate()
    {
        if (AllowedAttributeNames.IsEmpty && AllowedAttributePrefixes.IsDefaultOrEmpty)
            throw new InvalidOperationException("At least one telemetry attribute must be explicitly allowed.");
        if (SensitiveNameFragments.IsDefaultOrEmpty)
            throw new InvalidOperationException("Sensitive telemetry name fragments are required.");
        if (MaximumStringLength is < 1 or > 4096)
            throw new InvalidOperationException("Telemetry string length must be between 1 and 4096 characters.");
        ArgumentException.ThrowIfNullOrWhiteSpace(RedactedValue);
        return this;
    }
}
