using System.Collections.Immutable;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.VerticalSlice;

public sealed record VerticalSliceStageReceipt(
    Guid RunId,
    DeliveryStage Stage,
    string CompletedBySubjectId,
    string IdempotencyKey,
    string OutputReference,
    string OutputSha256Digest,
    bool PolicyGateSatisfied,
    bool HumanApprovalSatisfied,
    bool SupplyChainVerified,
    bool TelemetryEmitted,
    bool EnterpriseModelRegistered,
    bool ExternalEffectOccurred,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CompletedAt);
