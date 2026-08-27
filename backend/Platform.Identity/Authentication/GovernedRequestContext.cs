using System.Collections.Immutable;
using System.Security.Claims;
using Platform.Domain.Security;
using Platform.Identity.Access;

namespace Platform.Identity.Authentication;

public sealed record GovernedRequestContext(
    GovernedIdentity Identity,
    string AuthorizationEvidenceReference);

public sealed class GovernedRequestContextFactory
{
    public GovernedRequestContext Create(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("An authenticated governed identity is required.");

        var subjectId = RequiredClaim(principal, ClaimTypes.NameIdentifier, "sub");
        var tenantId = RequiredClaim(principal, "tenant_id");
        var issuer = RequiredClaim(principal, "iss");
        var clearanceValue = RequiredClaim(principal, "clearance");
        var authorizationEvidence = RequiredClaim(principal, "authorization_evidence");

        if (!Enum.TryParse<DataClassification>(clearanceValue, ignoreCase: false, out var clearance) ||
            !Enum.IsDefined(clearance))
            throw new UnauthorizedAccessException("The governed clearance claim is invalid.");

        var roles = Values(principal, ClaimTypes.Role, "role");
        var permissions = Values(principal, "permission");

        return new GovernedRequestContext(
            new GovernedIdentity(
                subjectId,
                tenantId,
                issuer,
                IsAuthenticated: true,
                clearance,
                roles,
                permissions,
                ImmutableDictionary<string, string>.Empty,
                CertificateThumbprint: null),
            authorizationEvidence);
    }

    private static string RequiredClaim(ClaimsPrincipal principal, params string[] claimTypes)
    {
        var value = claimTypes
            .Select(principal.FindFirst)
            .FirstOrDefault(claim => claim is not null)?.Value;

        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedAccessException($"Required governed claim '{claimTypes[^1]}' is missing.");

        return value;
    }

    private static ImmutableHashSet<string> Values(ClaimsPrincipal principal, params string[] claimTypes) =>
        claimTypes
            .SelectMany(principal.FindAll)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToImmutableHashSet(StringComparer.Ordinal);
}
