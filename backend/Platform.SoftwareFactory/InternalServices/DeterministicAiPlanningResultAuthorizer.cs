using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Identity.Access;

namespace Platform.SoftwareFactory.InternalService;

public sealed class DeterministicAiPlanningResultAuthorizer(
    IAccessPolicyEvaluator accessPolicyEvaluator) : IAiPlanningResultAuthorizer
{
    public Task<AiPlanningResultAuthorizationDecision> AuthorizeAsync(
        AiPlanningResultAuthorizationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request); cancellationToken.ThrowIfCancellationRequested(); Validate(request);
        var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
            request.Identity, request.Purpose, "ai-planning-candidate.read",
            $"{request.PlanningId:D}/{request.CandidateSha256Digest}", request.TenantId,
            request.MaximumClassification, request.RequiredRoles,
            ImmutableHashSet.Create(StringComparer.Ordinal, "developer.internal-service.ai-planning.create"),
            request.SubjectId, RequiresDistinctApprover: false));
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
            request.AuthorizationRequestId.ToString("D"), request.PlanningId.ToString("D"), request.TenantId,
            request.SubjectId, request.Purpose, request.Environment, request.CandidateSha256Digest,
            request.RuntimeProfile, request.PromptSha256Digest,
            string.Join(',', request.ContextSha256Digests.Order(StringComparer.OrdinalIgnoreCase)),
            string.Join(',', request.AllowedPackages.OrderBy(item => item.Name, StringComparer.Ordinal)
                .Select(item => $"{item.Kind}:{item.Name}:{item.Version}:{item.ContentDigest}")),
            access.IsAllowed.ToString(CultureInfo.InvariantCulture), access.Code))));
        var evidence = request.EvidenceReferences
            .Append($"evidence://ai-planning/result-authorization/{request.AuthorizationRequestId:D}/sha256/{digest}")
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        return Task.FromResult(new AiPlanningResultAuthorizationDecision(
            request.AuthorizationRequestId, request.PlanningId, request.TenantId,
            request.CandidateSha256Digest, access.IsAllowed, access.Code, evidence, request.RequestedAt));
    }

    private static void Validate(AiPlanningResultAuthorizationRequest request)
    {
        if (request.AuthorizationRequestId == Guid.Empty || request.PlanningId == Guid.Empty ||
            request.RequestedAt == default || request.RequiredRoles.IsEmpty ||
            request.ContextSha256Digests.IsEmpty || request.AllowedPackages.IsEmpty ||
            request.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("AI Planning result authorization request is incomplete.");
        ArgumentNullException.ThrowIfNull(request.Identity);
        if (!request.Identity.IsAuthenticated || !StringComparer.Ordinal.Equals(request.Identity.SubjectId, request.SubjectId) ||
            !StringComparer.Ordinal.Equals(request.Identity.TenantId, request.TenantId))
            throw new UnauthorizedAccessException("AI Planning result authorization identity is mismatched.");
        GovernedAiPlanningRequest.ValidateDigest(request.CandidateSha256Digest, "planning candidate");
        GovernedAiPlanningRequest.ValidateDigest(request.PromptSha256Digest, "planning prompt");
        foreach (var digest in request.ContextSha256Digests)
            GovernedAiPlanningRequest.ValidateDigest(digest, "planning context");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RuntimeProfile);
    }
}
