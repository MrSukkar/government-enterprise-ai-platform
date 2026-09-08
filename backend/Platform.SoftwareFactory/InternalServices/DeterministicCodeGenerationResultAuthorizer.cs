using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Identity.Access;

namespace Platform.SoftwareFactory.InternalService;

public sealed class DeterministicCodeGenerationResultAuthorizer(
    IAccessPolicyEvaluator accessPolicyEvaluator) : ICodeGenerationResultAuthorizer
{
    public Task<CodeGenerationResultAuthorizationDecision> AuthorizeAsync(
        CodeGenerationResultAuthorizationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request); cancellationToken.ThrowIfCancellationRequested(); Validate(request);
        if (!request.GeneratedFilePaths.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(request.AllowedOutputPaths))
            throw new UnauthorizedAccessException("Generated paths are outside the exact authorized result scope.");
        foreach (var path in request.GeneratedFilePaths) GovernedGeneratedPath.Validate(path);
        var access = accessPolicyEvaluator.Evaluate(new AccessRequest(request.Identity, request.Purpose,
            "code-generation-candidate.read", $"{request.GenerationId:D}/{request.CandidateSha256Digest}",
            request.TenantId, request.MaximumClassification, request.RequiredRoles,
            ImmutableHashSet.Create(StringComparer.Ordinal, "developer.internal-service.code-generation.create"),
            request.SubjectId, false));
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
            request.AuthorizationRequestId.ToString("D"), request.GenerationId.ToString("D"), request.PlanningId.ToString("D"),
            request.TenantId, request.SubjectId, request.Purpose, request.Environment, request.CandidateSha256Digest,
            request.RuntimeProfile, request.PromptSha256Digest,
            string.Join(',', request.ContextSha256Digests.Order(StringComparer.OrdinalIgnoreCase)),
            string.Join(',', request.ApprovedPackages.OrderBy(item => item.Name, StringComparer.Ordinal)
                .Select(item => $"{item.Kind}:{item.Name}:{item.Version}:{item.ContentDigest}")),
            string.Join(',', request.Constraints.Order(StringComparer.Ordinal)),
            string.Join(',', request.GeneratedFilePaths.Order(StringComparer.Ordinal)),
            access.IsAllowed.ToString(CultureInfo.InvariantCulture), access.Code))));
        var evidence = request.EvidenceReferences
            .Append($"evidence://code-generation/result-authorization/{request.AuthorizationRequestId:D}/sha256/{digest}")
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        return Task.FromResult(new CodeGenerationResultAuthorizationDecision(request.AuthorizationRequestId,
            request.GenerationId, request.PlanningId, request.TenantId, request.CandidateSha256Digest,
            access.IsAllowed, access.Code, evidence, request.RequestedAt));
    }

    private static void Validate(CodeGenerationResultAuthorizationRequest request)
    {
        if (request.AuthorizationRequestId == Guid.Empty || request.GenerationId == Guid.Empty || request.PlanningId == Guid.Empty ||
            request.RequestedAt == default || request.RequiredRoles.IsEmpty || request.ContextSha256Digests.IsEmpty ||
            request.ApprovedPackages.IsEmpty || request.AllowedOutputPaths.IsEmpty || request.GeneratedFilePaths.IsDefaultOrEmpty ||
            request.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Code Generation result authorization request is incomplete.");
        if (!request.Identity.IsAuthenticated || !StringComparer.Ordinal.Equals(request.Identity.SubjectId, request.SubjectId) ||
            !StringComparer.Ordinal.Equals(request.Identity.TenantId, request.TenantId))
            throw new UnauthorizedAccessException("Code Generation result authorization identity is mismatched.");
        GovernedAiPlanningRequest.ValidateDigest(request.CandidateSha256Digest, "code candidate");
        GovernedAiPlanningRequest.ValidateDigest(request.PromptSha256Digest, "Code Generation prompt");
        foreach (var digest in request.ContextSha256Digests) GovernedAiPlanningRequest.ValidateDigest(digest, "Code Generation context");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Purpose); ArgumentException.ThrowIfNullOrWhiteSpace(request.Environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RuntimeProfile);
    }
}
