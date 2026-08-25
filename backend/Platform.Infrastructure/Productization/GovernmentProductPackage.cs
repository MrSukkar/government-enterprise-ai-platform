using System.Collections.Immutable;
using Platform.Infrastructure.Sovereignty;

namespace Platform.Infrastructure.Productization;

public sealed record GovernmentProductPackage(
    Guid RequestId,
    Guid ProductId,
    string ProductVersion,
    string TenantId,
    string JurisdictionCode,
    DeploymentTopology Topology,
    string ManifestSha256Digest,
    string PolicyAuthorityReference,
    string IdentityAuthorityReference,
    ImmutableArray<string> ComplianceControlIds,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RegisteredAt);
