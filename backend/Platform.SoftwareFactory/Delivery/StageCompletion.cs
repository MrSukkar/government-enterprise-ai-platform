using System.Collections.Immutable;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.Delivery;

public sealed record StageCompletion(
    DeliveryStage Stage,
    StageResult Result,
    string CompletedBySubjectId,
    string EvidenceReference,
    DateTimeOffset CompletedAt,
    ImmutableArray<PackageUseDecision> PackageDecisions)
{
    public StageCompletion Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(CompletedBySubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(EvidenceReference);
        return this;
    }
}
