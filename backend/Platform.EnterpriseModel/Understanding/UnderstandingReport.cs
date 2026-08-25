using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.EnterpriseModel.Understanding;

public sealed record UnderstandingReport(
    Guid RequestId,
    string Summary,
    DataClassification SummaryClassification,
    ImmutableArray<UnderstandingClaim> Claims,
    string AnalysisEvidenceReference,
    DateTimeOffset CompletedAt)
{
    public bool IsExecutable => false;
}
