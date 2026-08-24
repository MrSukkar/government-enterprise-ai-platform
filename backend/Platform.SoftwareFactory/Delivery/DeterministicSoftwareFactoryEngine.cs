namespace Platform.SoftwareFactory.Delivery;

public sealed class DeterministicSoftwareFactoryEngine : ISoftwareFactoryEngine
{
    private static readonly DeliveryStage[] ApprovedSequence = Enum.GetValues<DeliveryStage>();

    public DeliveryTransitionDecision CompleteStage(SoftwareDeliveryRun run, StageCompletion completion)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(completion);
        run.Validate();
        completion.Validate();

        if (run.IsComplete)
            return DeliveryTransitionDecision.Deny("workflow_complete", "A completed delivery run cannot advance.", run);

        var expectedStage = run.CurrentStage is null
            ? DeliveryStage.Intent
            : ApprovedSequence[Array.IndexOf(ApprovedSequence, run.CurrentStage.Value) + 1];

        if (completion.Stage != expectedStage)
            return DeliveryTransitionDecision.Deny("stage_order_denied", $"Expected stage '{expectedStage}'.", run);
        if (completion.Result != StageResult.Passed)
            return DeliveryTransitionDecision.Deny("stage_not_passed", "Only a passed stage may advance the workflow.", run);
        if (completion.Stage == DeliveryStage.ApprovedPackages && completion.PackageDecisions.Any(decision => !decision.IsAllowed))
            return DeliveryTransitionDecision.Deny("package_denied", "Every declared package must be institutionally approved.", run);
        if (completion.Stage == DeliveryStage.HumanReview &&
            StringComparer.Ordinal.Equals(completion.CompletedBySubjectId, run.InitiatorSubjectId))
            return DeliveryTransitionDecision.Deny("independent_review_required", "The initiator cannot perform the human review.", run);
        if (run.CurrentStage is not null && completion.CompletedAt < run.History[^1].CompletedAt)
            return DeliveryTransitionDecision.Deny("time_order_denied", "Stage completion time cannot move backward.", run);

        var advanced = run with { History = run.History.Add(completion) };
        return DeliveryTransitionDecision.Allow(advanced);
    }
}
