using System.Collections.Immutable;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.VerticalSlice;

public sealed record VerticalSliceExecutionContext(
    InternalServiceVerticalSliceRequest Request,
    DeliveryStage Stage,
    int Ordinal,
    long ExpectedVersion,
    string IdempotencyKey,
    ImmutableArray<VerticalSliceStageReceipt> PriorReceipts);
