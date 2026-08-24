namespace Platform.Identity.Access;

public interface IAccessPolicyEvaluator
{
    AccessDecision Evaluate(AccessRequest request);
}
