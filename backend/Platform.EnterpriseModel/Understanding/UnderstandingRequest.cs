using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Understanding;

public sealed record UnderstandingRequest(
    Guid Id,
    string SubjectId,
    ImmutableHashSet<string> Permissions,
    string TenantId,
    ImmutableHashSet<EnterpriseObjectId> ObjectScope,
    DataClassification MaximumClassification,
    string Purpose,
    DateTimeOffset RequestedAt)
{
    public UnderstandingRequest Validate()
    {
        if (Id == Guid.Empty) throw new InvalidOperationException("Understanding request identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentNullException.ThrowIfNull(Permissions);
        ArgumentNullException.ThrowIfNull(ObjectScope);
        if (!Permissions.Contains("enterprise.understanding.read"))
            throw new UnauthorizedAccessException("The enterprise.understanding.read permission is required.");
        if (ObjectScope.IsEmpty) throw new InvalidOperationException("Understanding requires an explicit object scope.");
        if (RequestedAt == default) throw new InvalidOperationException("Understanding request time is required.");
        return this;
    }
}
