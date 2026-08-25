using System.Collections.Immutable;

namespace Platform.EnterpriseModel.Intelligence;

public interface IProactiveIntelligenceAnalyzer
{
    Task<ImmutableArray<ProactiveFindingCandidate>> AnalyzeAsync(
        ProactiveIntelligenceRequest request,
        ProactiveIntelligenceSnapshot snapshot,
        CancellationToken cancellationToken);
}
