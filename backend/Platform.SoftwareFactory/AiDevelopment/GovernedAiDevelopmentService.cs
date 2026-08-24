using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.AiDevelopment;

public sealed class GovernedAiDevelopmentService(
    IAiDevelopmentRuntime runtime,
    IAiOutputEvaluator evaluator)
{
    public async Task<EvaluatedAiCandidate> ProduceCandidateAsync(
        AiDevelopmentRequest request,
        CancellationToken cancellationToken)
    {
        request.Validate();
        EnsureWorkflowBoundary(request);

        var candidate = await runtime.ExecuteAsync(request, cancellationToken);
        candidate.Validate();
        var evaluation = await evaluator.EvaluateAsync(request, candidate, cancellationToken);
        evaluation.Validate();

        return new EvaluatedAiCandidate(candidate, evaluation);
    }

    private static void EnsureWorkflowBoundary(AiDevelopmentRequest request)
    {
        var requiredPreviousStage = request.TaskKind switch
        {
            AiDevelopmentTaskKind.Planning => DeliveryStage.ApprovedPackages,
            AiDevelopmentTaskKind.CodeGeneration => DeliveryStage.AiPlanning,
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        if (request.Run.CurrentStage != requiredPreviousStage)
        {
            throw new InvalidOperationException(
                $"AI task '{request.TaskKind}' requires completed stage '{requiredPreviousStage}'.");
        }
    }
}
