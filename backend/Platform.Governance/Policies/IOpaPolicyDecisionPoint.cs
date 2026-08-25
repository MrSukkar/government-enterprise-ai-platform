namespace Platform.Governance.Policies;

public interface IOpaPolicyDecisionPoint
{
    Task<OpaPolicyDecision> EvaluateAsync(OpaPolicyInput input, CancellationToken cancellationToken);
}
