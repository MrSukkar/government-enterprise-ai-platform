using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Intelligence;

public sealed record ProactiveIntelligenceRequest(
    Guid RequestId,
    string TenantId,
    string SubjectId,
    ImmutableHashSet<string> Permissions,
    string Purpose,
    string Environment,
    ImmutableHashSet<EnterpriseObjectId> AuthorizedObjectScope,
    DataClassification MaximumClassification,
    IntelligencePolicyReference DetectionPolicy,
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd,
    DateTimeOffset RequestedAt)
{
    public ProactiveIntelligenceRequest Validate()
    {
        if (RequestId == Guid.Empty) throw new InvalidOperationException("Intelligence request identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentNullException.ThrowIfNull(Permissions);
        ArgumentNullException.ThrowIfNull(AuthorizedObjectScope);
        ArgumentNullException.ThrowIfNull(DetectionPolicy);
        if (!Permissions.Contains("enterprise.intelligence.evaluate"))
            throw new UnauthorizedAccessException("The enterprise.intelligence.evaluate permission is required.");
        if (AuthorizedObjectScope.IsEmpty) throw new UnauthorizedAccessException("Proactive intelligence requires explicit scope.");
        DetectionPolicy.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, DetectionPolicy.Environment))
            throw new InvalidOperationException("Detection policy environment does not match the request.");
        if (WindowStart == default || WindowEnd <= WindowStart || RequestedAt < WindowEnd)
            throw new InvalidOperationException("Intelligence observation window is invalid.");
        return this;
    }
}
