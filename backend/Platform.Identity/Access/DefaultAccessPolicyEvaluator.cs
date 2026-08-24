namespace Platform.Identity.Access;

public sealed class DefaultAccessPolicyEvaluator : IAccessPolicyEvaluator
{
    public AccessDecision Evaluate(AccessRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Identity.IsAuthenticated)
        {
            return AccessDecision.Deny("identity_required", "An authenticated governed identity is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Purpose))
        {
            return AccessDecision.Deny("purpose_required", "A declared purpose is required.");
        }

        if (!StringComparer.Ordinal.Equals(request.Identity.TenantId, request.ResourceTenantId))
        {
            return AccessDecision.Deny("tenant_scope_denied", "The resource is outside the active tenant scope.");
        }

        if (request.Identity.Clearance < request.ResourceClassification)
        {
            return AccessDecision.Deny("classification_denied", "Identity clearance is insufficient for the resource classification.");
        }

        if (!request.RequiredRoles.IsSubsetOf(request.Identity.Roles))
        {
            return AccessDecision.Deny("role_denied", "One or more required roles are absent.");
        }

        if (request.RequiresDistinctApprover &&
            StringComparer.Ordinal.Equals(request.Identity.SubjectId, request.InitiatorSubjectId))
        {
            return AccessDecision.Deny("separation_of_duties_denied", "The initiator cannot approve this action.");
        }

        return AccessDecision.Allow();
    }
}
