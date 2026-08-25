namespace Platform.EnterpriseModel.Understanding;

public interface IUnderstandingAnalyzer
{
    Task<UnderstandingCandidate> AnalyzeAsync(
        UnderstandingRequest request,
        UnderstandingSnapshot snapshot,
        CancellationToken cancellationToken);
}
