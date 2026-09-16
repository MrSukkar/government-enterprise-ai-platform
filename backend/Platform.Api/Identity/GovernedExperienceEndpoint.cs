using System.Globalization;
using System.Security.Claims;
using Platform.Identity.Authentication;

namespace Platform.Api.Identity;

internal static class GovernedExperienceEndpoint
{
    internal static IEndpointConventionBuilder MapGovernedExperienceContext(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGet("/api/v1/identity/context", (
                HttpContext httpContext,
                GovernedRequestContextFactory contextFactory) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var purpose = RequiredClaim(httpContext.User, "purpose");
                    var persona = RequiredClaim(httpContext.User, "persona");
                    var issuedAt = UnixTimeClaim(httpContext.User, "iat");
                    var expiresAt = UnixTimeClaim(httpContext.User, "exp");

                    if (expiresAt <= issuedAt || expiresAt <= DateTimeOffset.UtcNow)
                        throw new UnauthorizedAccessException("The governed identity context is expired.");

                    return Results.Ok(new GovernedExperienceContextResponse(
                        context.Identity.SubjectId,
                        context.Identity.TenantId,
                        persona,
                        purpose,
                        context.Identity.Permissions.Order(StringComparer.Ordinal).ToArray(),
                        context.AuthorizationEvidenceReference,
                        issuedAt,
                        expiresAt));
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or UnauthorizedAccessException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Governed identity context denied.");
                }
            })
            .WithName("GetGovernedExperienceContext")
            .WithTags("Identity")
            .WithSummary("Read the server-established governed browser context.")
            .WithDescription("Returns identity, tenant, persona, purpose, permissions, authorization evidence, and validity only after bearer-token validation. It grants no permission and performs no mutation.")
            .Produces<GovernedExperienceContextResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization();
    }

    private static string RequiredClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirst(claimType)?.Value;
        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedAccessException($"Required governed claim '{claimType}' is missing.");
        return value;
    }

    private static DateTimeOffset UnixTimeClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = RequiredClaim(principal, claimType);
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            throw new UnauthorizedAccessException($"Governed claim '{claimType}' is invalid.");
        return DateTimeOffset.FromUnixTimeSeconds(seconds);
    }

    private sealed record GovernedExperienceContextResponse(
        string SubjectId,
        string TenantId,
        string Persona,
        string Purpose,
        IReadOnlyList<string> Permissions,
        string AuthorizationEvidenceReference,
        DateTimeOffset IssuedAt,
        DateTimeOffset ExpiresAt);
}
