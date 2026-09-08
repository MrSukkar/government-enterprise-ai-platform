using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Identity.Access;

namespace Platform.SoftwareFactory.InternalService;

public sealed class DeterministicSecurityValidationResultAuthorizer(
    IAccessPolicyEvaluator accessPolicyEvaluator) : ISecurityValidationResultAuthorizer
{
    public Task<SecurityValidationResultAuthorizationDecision> AuthorizeAsync(
        SecurityValidationResultAuthorizationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request); cancellationToken.ThrowIfCancellationRequested(); Validate(request);
        var access = accessPolicyEvaluator.Evaluate(new AccessRequest(request.Identity, request.Purpose,
            "security-validation-report.read", $"{request.ValidationId:D}/{request.SecurityReportSha256Digest}",
            request.TenantId, request.MaximumClassification, request.RequiredRoles,
            ImmutableHashSet.Create(StringComparer.Ordinal, "developer.internal-service.security-validation.create"),
            request.SubjectId, false));
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
            request.AuthorizationRequestId.ToString("D"), request.ValidationId.ToString("D"),
            request.GenerationId.ToString("D"), request.TenantId, request.SubjectId, request.Purpose,
            request.Environment, request.CandidateSha256Digest, request.StaticReportSha256Digest,
            request.SecurityReportSha256Digest, string.Join(',', request.ControlIds.Order(StringComparer.Ordinal)),
            access.IsAllowed.ToString(CultureInfo.InvariantCulture), access.Code))));
        var evidence = request.EvidenceReferences
            .Append($"evidence://security-validation/result-authorization/{request.AuthorizationRequestId:D}/sha256/{digest}")
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        return Task.FromResult(new SecurityValidationResultAuthorizationDecision(request.AuthorizationRequestId,
            request.ValidationId, request.GenerationId, request.TenantId, request.SecurityReportSha256Digest,
            access.IsAllowed, access.Code, evidence, request.RequestedAt));
    }

    private static void Validate(SecurityValidationResultAuthorizationRequest request)
    {
        if (request.AuthorizationRequestId == Guid.Empty || request.ValidationId == Guid.Empty ||
            request.GenerationId == Guid.Empty || request.RequestedAt == default || request.RequiredRoles.IsEmpty ||
            request.ControlIds.IsDefaultOrEmpty || request.ControlIds.Distinct(StringComparer.Ordinal).Count() != request.ControlIds.Length ||
            request.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Security result authorization request is incomplete.");
        if (!request.Identity.IsAuthenticated || !StringComparer.Ordinal.Equals(request.Identity.SubjectId, request.SubjectId) ||
            !StringComparer.Ordinal.Equals(request.Identity.TenantId, request.TenantId) ||
            request.Identity.Clearance < request.MaximumClassification)
            throw new UnauthorizedAccessException("Security result authorization identity is invalid.");
        GovernedAiPlanningRequest.ValidateDigest(request.CandidateSha256Digest, "code candidate");
        GovernedAiPlanningRequest.ValidateDigest(request.StaticReportSha256Digest, "Static report");
        GovernedAiPlanningRequest.ValidateDigest(request.SecurityReportSha256Digest, "Security report");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Environment);
    }
}
