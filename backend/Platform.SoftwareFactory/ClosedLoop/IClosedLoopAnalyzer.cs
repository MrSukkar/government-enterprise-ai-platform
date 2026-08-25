using System.Collections.Immutable;

namespace Platform.SoftwareFactory.ClosedLoop;

public interface IClosedLoopAnalyzer
{
    Task<ImmutableArray<ImprovementCandidate>> AnalyzeAsync(
        ClosedLoopEvaluationRequest request,
        ClosedLoopContext context,
        CancellationToken cancellationToken);
}
