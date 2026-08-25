using System.Collections.Immutable;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.VerticalSlice;

public sealed record VerticalSliceRun(
    InternalServiceVerticalSliceRequest Request,
    long Version,
    ImmutableArray<VerticalSliceStageReceipt> Receipts,
    DateTimeOffset UpdatedAt)
{
    public DeliveryStage? CurrentStage => Receipts.IsDefaultOrEmpty ? null : Receipts[^1].Stage;
    public bool IsComplete => CurrentStage == DeliveryStage.Evidence;

    public VerticalSliceRun Validate()
    {
        ArgumentNullException.ThrowIfNull(Request);
        Request.Validate();
        if (Version < 0 || Receipts.IsDefault || Version != Receipts.Length)
            throw new InvalidOperationException("Vertical slice version and receipt sequence are invalid.");
        var stages = Enum.GetValues<DeliveryStage>();
        if (!Receipts.Select(item => item.Stage).SequenceEqual(stages.Take(Receipts.Length)))
            throw new InvalidOperationException("Vertical slice receipts are not in approved order.");
        if (UpdatedAt < Request.RequestedAt) throw new InvalidOperationException("Vertical slice time is invalid.");
        return this;
    }
}
