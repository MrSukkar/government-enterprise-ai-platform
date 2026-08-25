using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.EnterpriseModel.Understanding;

public sealed record UnderstandingCandidate(
    Guid RequestId,
    string Summary,
    DataClassification SummaryClassification,
    ImmutableArray<UnderstandingClaim> Claims,
    string AnalysisEvidenceReference,
    DateTimeOffset GeneratedAt)
{
    public bool IsExecutable => false;
}
