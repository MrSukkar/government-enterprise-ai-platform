namespace Platform.SoftwareFactory.Delivery;

public interface ISoftwareFactoryEngine
{
    DeliveryTransitionDecision CompleteStage(SoftwareDeliveryRun run, StageCompletion completion);
}
