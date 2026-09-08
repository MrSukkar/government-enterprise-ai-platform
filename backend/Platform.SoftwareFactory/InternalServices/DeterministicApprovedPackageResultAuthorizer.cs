using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Identity.Access;

namespace Platform.SoftwareFactory.InternalService;

public sealed class DeterministicApprovedPackageResultAuthorizer(
    IAccessPolicyEvaluator accessPolicyEvaluator) : IApprovedPackageResultAuthorizer
{
    public Task<ApprovedPackageResultAuthorizationDecision> AuthorizeAsync(
        ApprovedPackageResultAuthorizationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request); cancellationToken.ThrowIfCancellationRequested(); Validate(request);
        var scoped = request.AllowedCoordinates.Contains(request.Coordinate);
        var access = scoped ? accessPolicyEvaluator.Evaluate(new AccessRequest(
            request.Identity, request.Purpose, request.Action,
            $"{request.Coordinate.Kind}/{request.Coordinate.Name}/{request.Coordinate.Version}/{request.Coordinate.ContentDigest}",
            request.TenantId, request.Classification, request.RequiredRoles,
            ImmutableHashSet.Create(StringComparer.Ordinal, "developer.internal-service.packages.select"),
            request.SubjectId, RequiresDistinctApprover: false))
            : AccessDecision.Deny("approved_package_scope_denied", "Package is outside the OPA-authorized scope.");
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
            request.AuthorizationRequestId.ToString("D"), request.SelectionId.ToString("D"), request.TenantId,
            request.SubjectId, request.Purpose, request.Action, request.Coordinate.Kind, request.Coordinate.Name,
            request.Coordinate.Version, request.Coordinate.ContentDigest, request.Classification, request.Environment,
            access.IsAllowed.ToString(CultureInfo.InvariantCulture), access.Code))));
        var evidence = request.EvidenceReferences
            .Append($"evidence://approved-packages/authorization/{request.AuthorizationRequestId:D}/sha256/{digest}")
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        return Task.FromResult(new ApprovedPackageResultAuthorizationDecision(
            request.AuthorizationRequestId, request.SelectionId, request.TenantId, request.Coordinate,
            access.IsAllowed, access.Code, evidence, request.RequestedAt));
    }
    private static void Validate(ApprovedPackageResultAuthorizationRequest request)
    {
        if (request.AuthorizationRequestId == Guid.Empty || request.SelectionId == Guid.Empty ||
            request.Action != "approved-package.read" || request.RequestedAt == default ||
            request.RequiredRoles.IsEmpty || request.AllowedCoordinates.IsEmpty || request.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Approved Package authorization request is incomplete.");
        ArgumentNullException.ThrowIfNull(request.Identity);
        if (!request.Identity.IsAuthenticated || !StringComparer.Ordinal.Equals(request.Identity.SubjectId, request.SubjectId) ||
            !StringComparer.Ordinal.Equals(request.Identity.TenantId, request.TenantId))
            throw new UnauthorizedAccessException("Approved Package authorization identity is mismatched.");
        GovernedApprovedPackagesSelectionRequest.ValidateExactCoordinate(request.Coordinate);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Purpose); ArgumentException.ThrowIfNullOrWhiteSpace(request.Environment);
    }
}
