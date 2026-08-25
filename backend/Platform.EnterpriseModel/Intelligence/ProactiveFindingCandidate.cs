using System.Collections.Immutable;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Intelligence;

public enum ProactiveFindingDisposition
{
    Observe = 0,
    Investigate = 1,
    RecommendGovernedAction = 2
}

public sealed record ProactiveFindingCandidate(
    EnterpriseObjectId ObjectId,
    ImmutableArray<Guid> SignalIds,
    ProactiveFindingDisposition Disposition,
    string Title,
    string Rationale,
    string? RecommendedActionName,
    decimal Confidence,
    ImmutableArray<string> EvidenceReferences);
