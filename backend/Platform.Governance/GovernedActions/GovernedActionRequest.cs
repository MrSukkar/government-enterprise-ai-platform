using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.Governance.Policies;

namespace Platform.Governance.GovernedActions;

public sealed record GovernedActionRequest(
    Guid RequestId,
    string TenantId,
    string RequestedBySubjectId,
    string ApprovedBySubjectId,
    ImmutableHashSet<string> Permissions,
    string Purpose,
    string Environment,
    DataClassification Classification,
    string ActionName,
    string TargetResource,
    ImmutableSortedDictionary<string, string> Parameters,
    ImmutableArray<string> EvidenceReferences,
    string ApprovalEvidenceReference,
    SignedPolicyBundleReference PolicyBundle,
    DateTimeOffset RequestedAt)
{
    public GovernedActionRequest Validate()
    {
        if (RequestId == Guid.Empty) throw new InvalidOperationException("Governed action identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(RequestedBySubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ApprovedBySubjectId);
        ArgumentNullException.ThrowIfNull(Permissions);
        if (!Permissions.Contains("governance.action.execute"))
            throw new UnauthorizedAccessException("The governance.action.execute permission is required.");
        if (StringComparer.Ordinal.Equals(RequestedBySubjectId, ApprovedBySubjectId))
            throw new InvalidOperationException("Governed actions require separation of duties.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(ActionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(TargetResource);
        ArgumentException.ThrowIfNullOrWhiteSpace(ApprovalEvidenceReference);
        ArgumentNullException.ThrowIfNull(Parameters);
        if (EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Governed actions require source evidence.");
        foreach (var item in Parameters) { ArgumentException.ThrowIfNullOrWhiteSpace(item.Key); ArgumentNullException.ThrowIfNull(item.Value); }
        foreach (var item in EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(item);
        ArgumentNullException.ThrowIfNull(PolicyBundle);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment))
            throw new InvalidOperationException("Policy bundle environment does not match the action environment.");
        if (RequestedAt == default || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Governed action time is invalid for the active policy bundle.");
        return this;
    }
}
