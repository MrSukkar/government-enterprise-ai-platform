using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using Platform.Web.Foundation;
using Platform.Web.FrontDoor;

namespace Platform.Web.Authentication;

public sealed class GovernedExperienceContextClient(
    HttpClient httpClient,
    ExperienceContext experienceContext)
{
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/v1/identity/context", cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            experienceContext.Clear();
            throw new UnauthorizedAccessException("The server denied the governed browser context.");
        }

        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<GovernedExperienceContextResponse>(cancellationToken)
            ?? throw new InvalidOperationException("The governed browser context response was empty.");

        experienceContext.ApplyServerAuthorizedContext(new GovernedExperienceContext(
            value.SubjectId,
            value.TenantId,
            value.Persona,
            value.Purpose,
            value.Permissions.ToImmutableHashSet(StringComparer.Ordinal),
            value.AuthorizationEvidenceReference,
            value.IssuedAt,
            value.ExpiresAt), DateTimeOffset.UtcNow);
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
