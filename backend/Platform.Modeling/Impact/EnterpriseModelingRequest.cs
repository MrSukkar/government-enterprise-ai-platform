using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;

namespace Platform.Modeling.Impact;

public sealed record EnterpriseModelingRequest(
    Guid RequestId,
    string TenantId,
    string SubjectId,
    ImmutableHashSet<string> Permissions,
    string Purpose,
    ImmutableHashSet<EnterpriseObjectId> AuthorizedObjectScope,
    DataClassification MaximumClassification,
    int MaximumTraversalDepth,
    EnterpriseChangeProposal Change,
    DateTimeOffset RequestedAt)
{
    public EnterpriseModelingRequest Validate()
    {
        if (RequestId == Guid.Empty) throw new InvalidOperationException("Modeling request identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentNullException.ThrowIfNull(Permissions);
        ArgumentNullException.ThrowIfNull(AuthorizedObjectScope);
        ArgumentNullException.ThrowIfNull(Change);
        if (!Permissions.Contains("enterprise.modeling.analyze"))
            throw new UnauthorizedAccessException("The enterprise.modeling.analyze permission is required.");
        if (AuthorizedObjectScope.IsEmpty || !AuthorizedObjectScope.Contains(Change.TargetObjectId))
            throw new UnauthorizedAccessException("Change target must be inside explicit authorized scope.");
        if (MaximumTraversalDepth <= 0) throw new InvalidOperationException("Traversal depth must be explicitly bounded.");
        Change.Validate();
        if (RequestedAt == default) throw new InvalidOperationException("Modeling request time is required.");
        return this;
    }
}
