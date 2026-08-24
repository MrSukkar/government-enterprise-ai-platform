using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.Identity.Access;

public sealed record GovernedIdentity(
    string SubjectId,
    string TenantId,
    string Issuer,
    bool IsAuthenticated,
    DataClassification Clearance,
    ImmutableHashSet<string> Roles,
    ImmutableDictionary<string, string> Attributes,
    string? CertificateThumbprint)
{
    public bool HasRole(string role) => Roles.Contains(role);
}
