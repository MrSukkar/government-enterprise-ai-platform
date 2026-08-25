using System.Collections.Immutable;
using Platform.Infrastructure.Sovereignty;

namespace Platform.Infrastructure.Productization;

public sealed record GovernmentProductizationRequest(
    Guid RequestId,
    string TenantId,
    string PublishedBySubjectId,
    string ApprovedBySubjectId,
    ImmutableHashSet<string> Permissions,
    JurisdictionProfile Jurisdiction,
    SovereignDeploymentProfile DeploymentProfile,
    GovernmentProductManifest Manifest,
    string ApprovalEvidenceReference,
    DateTimeOffset RequestedAt)
{
    public GovernmentProductizationRequest Validate()
    {
        if (RequestId == Guid.Empty) throw new InvalidOperationException("Productization request identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(PublishedBySubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ApprovedBySubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ApprovalEvidenceReference);
        ArgumentNullException.ThrowIfNull(Permissions);
        ArgumentNullException.ThrowIfNull(Jurisdiction);
        ArgumentNullException.ThrowIfNull(DeploymentProfile);
        ArgumentNullException.ThrowIfNull(Manifest);
        if (!Permissions.Contains("government.product.publish"))
            throw new UnauthorizedAccessException("The government.product.publish permission is required.");
        if (StringComparer.Ordinal.Equals(PublishedBySubjectId, ApprovedBySubjectId))
            throw new InvalidOperationException("Government product publication requires separation of duties.");
        Jurisdiction.Validate();
        DeploymentProfile.Validate();
        Manifest.Validate();
        if (!StringComparer.Ordinal.Equals(TenantId, DeploymentProfile.TenantId) ||
            !Jurisdiction.AllowedTopologies.Contains(DeploymentProfile.Topology))
            throw new UnauthorizedAccessException("Deployment profile is not permitted by the jurisdiction.");
        if (RequestedAt < Manifest.ReleasedAt)
            throw new InvalidOperationException("Productization request cannot precede the release.");
        return this;
    }
}
