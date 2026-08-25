using Platform.Governance.Policies;

namespace Platform.Governance.GovernedActions;

public sealed record GovernedActionOutcome(
    Guid RequestId,
    OpaDecisionOutcome Decision,
    bool Executed,
    GovernedActionResult? Result,
    string DecisionEvidenceReference);
