using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Identity.Access;

namespace Platform.SoftwareFactory.InternalService;

public sealed class DeterministicSandboxResultAuthorizer(
    IAccessPolicyEvaluator accessPolicyEvaluator) : ISandboxResultAuthorizer
{
    public Task<SandboxResultAuthorizationDecision> AuthorizeAsync(
        SandboxResultAuthorizationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request); cancellationToken.ThrowIfCancellationRequested(); Validate(request);
        var access = accessPolicyEvaluator.Evaluate(new AccessRequest(request.Identity, request.Purpose,
            "sandbox-result.read", $"{request.ExecutionId:D}/{request.ResultSha256Digest}", request.TenantId,
            request.MaximumClassification, request.RequiredRoles,
            ImmutableHashSet.Create(StringComparer.Ordinal, "developer.internal-service.sandbox.execute"),
            request.SubjectId, false));
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
            request.AuthorizationRequestId.ToString("D"), request.ExecutionId.ToString("D"), request.TenantId,
            request.SubjectId, request.Purpose, request.Environment, request.CandidateSha256Digest,
            request.SecurityReportSha256Digest, request.ResultSha256Digest,
            request.SandboxImage.Kind, request.SandboxImage.Name, request.SandboxImage.Version,
            request.SandboxImage.ContentDigest, access.IsAllowed.ToString(CultureInfo.InvariantCulture), access.Code))));
        var evidence = request.EvidenceReferences
            .Append($"evidence://sandbox/result-authorization/{request.AuthorizationRequestId:D}/sha256/{digest}")
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        return Task.FromResult(new SandboxResultAuthorizationDecision(request.AuthorizationRequestId,
            request.ExecutionId, request.TenantId, request.ResultSha256Digest, access.IsAllowed,
            access.Code, evidence, request.RequestedAt));
    }

    private static void Validate(SandboxResultAuthorizationRequest request)
    {
        if (request.AuthorizationRequestId == Guid.Empty || request.ExecutionId == Guid.Empty ||
            request.RequestedAt == default || request.RequiredRoles.IsEmpty || request.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Sandbox result authorization request is incomplete.");
        if (!request.Identity.IsAuthenticated || !StringComparer.Ordinal.Equals(request.Identity.SubjectId, request.SubjectId) ||
            !StringComparer.Ordinal.Equals(request.Identity.TenantId, request.TenantId) ||
            request.Identity.Clearance < request.MaximumClassification)
            throw new UnauthorizedAccessException("Sandbox result authorization identity is invalid.");
        GovernedAiPlanningRequest.ValidateDigest(request.CandidateSha256Digest, "code candidate");
        GovernedAiPlanningRequest.ValidateDigest(request.SecurityReportSha256Digest, "Security report");
        GovernedAiPlanningRequest.ValidateDigest(request.ResultSha256Digest, "Sandbox result");
        request.SandboxImage.Validate(); request.IsolationPolicy.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Purpose); ArgumentException.ThrowIfNullOrWhiteSpace(request.Environment);
        if (!request.Result.IsAccepted || request.EnvironmentReferences is null)
            throw new UnauthorizedAccessException("Sandbox result is not eligible for release.");
    }
}
