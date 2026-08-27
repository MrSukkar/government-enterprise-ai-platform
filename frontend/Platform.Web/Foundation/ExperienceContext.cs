using System.Collections.Immutable;
using Platform.Web.FrontDoor;

namespace Platform.Web.Foundation;

public sealed class ExperienceContext
{
    public const string UnauthenticatedPersona = "Sign-in required";
    public string Persona { get; private set; } = UnauthenticatedPersona;
    public string Purpose { get; private set; } = "No governed purpose established";
    public string? TenantId { get; private set; }
    public ImmutableHashSet<string> Permissions { get; private set; } = [];
    public string? AuthorizationEvidenceReference { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsGovernedIdentityEstablished { get; private set; }

    public void ApplyServerAuthorizedContext(GovernedExperienceContext context, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Validate(now);
        Persona = context.Persona;
        Purpose = context.Purpose;
        TenantId = context.TenantId;
        Permissions = context.Permissions;
        AuthorizationEvidenceReference = context.AuthorizationEvidenceReference;
        ExpiresAt = context.ExpiresAt;
        IsGovernedIdentityEstablished = true;
    }

    public bool CanAccess(FrontDoorDestination destination) =>
        IsGovernedIdentityEstablished && Permissions.Contains(destination.RequiredPermission);

    public bool HasPermission(string permission) =>
        IsGovernedIdentityEstablished && Permissions.Contains(permission);

    public void Clear() =>
        (Persona, Purpose, TenantId, Permissions, AuthorizationEvidenceReference, ExpiresAt,
            IsGovernedIdentityEstablished) =
        (UnauthenticatedPersona, "No governed purpose established", null, [], null, null, false);
}
