namespace Platform.Integrations.Constitution;

public enum IntegrationSemanticDisposition
{
    Required,
    NotApplicable,
    Prohibited
}

public sealed record IntegrationSemanticRule(
    IntegrationSemanticDisposition Disposition,
    string? PolicyReference)
{
    public IntegrationSemanticRule Validate(string ruleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);
        if (!Enum.IsDefined(Disposition))
        {
            throw new ArgumentOutOfRangeException(ruleName, "Unknown semantic disposition fails closed.");
        }

        if (Disposition == IntegrationSemanticDisposition.Required)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(PolicyReference, ruleName);
        }
        else if (!string.IsNullOrWhiteSpace(PolicyReference))
        {
            throw new InvalidOperationException($"{ruleName} cannot bind policy when it is not required.");
        }

        return this;
    }
}

public sealed record IntegrationDeliverySemantics(
    IntegrationSemanticRule Idempotency,
    IntegrationSemanticRule Ordering,
    IntegrationSemanticRule Retry,
    IntegrationSemanticRule Timeout,
    IntegrationSemanticRule Compensation,
    IntegrationSemanticRule Replay)
{
    public IntegrationDeliverySemantics Validate()
    {
        ArgumentNullException.ThrowIfNull(Idempotency);
        ArgumentNullException.ThrowIfNull(Ordering);
        ArgumentNullException.ThrowIfNull(Retry);
        ArgumentNullException.ThrowIfNull(Timeout);
        ArgumentNullException.ThrowIfNull(Compensation);
        ArgumentNullException.ThrowIfNull(Replay);
        Idempotency.Validate(nameof(Idempotency));
        Ordering.Validate(nameof(Ordering));
        Retry.Validate(nameof(Retry));
        Timeout.Validate(nameof(Timeout));
        Compensation.Validate(nameof(Compensation));
        Replay.Validate(nameof(Replay));
        return this;
    }
}
