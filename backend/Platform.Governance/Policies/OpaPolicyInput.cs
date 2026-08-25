using Platform.Governance.GovernedActions;

namespace Platform.Governance.Policies;

public sealed record OpaPolicyInput(
    Guid DecisionRequestId,
    GovernedActionRequest Action,
    PolicyBundleVerification VerifiedPolicyBundle,
    DateTimeOffset EvaluatedAt);
