using System.Collections.Immutable;

namespace Platform.SoftwareFactory.Delivery;

public sealed record SoftwareDeliveryRun
{
    public required Guid Id { get; init; }
    public required string TenantId { get; init; }
    public required string InitiatorSubjectId { get; init; }
    public required string Intent { get; init; }
    public required ImmutableArray<StageCompletion> History { get; init; }

    public DeliveryStage? CurrentStage => History.IsDefaultOrEmpty ? null : History[^1].Stage;
    public bool IsComplete => CurrentStage == DeliveryStage.Evidence && History[^1].Result == StageResult.Passed;

    public SoftwareDeliveryRun Validate()
    {
        if (Id == Guid.Empty) throw new InvalidOperationException("Delivery run identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(InitiatorSubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Intent);
        return this;
    }
}
