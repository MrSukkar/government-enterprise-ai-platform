namespace Platform.SoftwareFactory.Delivery;

public sealed record DeliveryTransitionDecision(
    bool IsAllowed,
    string Code,
    string Reason,
    SoftwareDeliveryRun Run)
{
    public static DeliveryTransitionDecision Allow(SoftwareDeliveryRun run) =>
        new(true, "transition_recorded", "The required stage was completed in sequence.", run);

    public static DeliveryTransitionDecision Deny(string code, string reason, SoftwareDeliveryRun run) =>
        new(false, code, reason, run);
}
