using System.Collections.Immutable;

namespace Platform.Web.FrontDoor;

public sealed record GovernedExperienceContext(
    string SubjectId,
    string TenantId,
    string Persona,
    string Purpose,
    ImmutableHashSet<string> Permissions,
    string AuthorizationEvidenceReference,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt)
{
    public void Validate(DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Persona);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentNullException.ThrowIfNull(Permissions);
        if (Permissions.IsEmpty || IssuedAt == default || ExpiresAt <= IssuedAt || now >= ExpiresAt)
            throw new UnauthorizedAccessException("Governed experience context is missing or expired.");
    }
}
