using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Identity.Access;

namespace Platform.SoftwareFactory.InternalService;

public sealed class DeterministicExistingArchitectureResultAuthorizer(
    IAccessPolicyEvaluator accessPolicyEvaluator) : IExistingArchitectureResultAuthorizer
{
    public Task<ExistingArchitectureResultAuthorizationDecision> AuthorizeAsync(
        ExistingArchitectureResultAuthorizationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        Validate(request);
        var scoped = request.AllowedSystemIds.Contains(request.SystemId) &&
            request.AllowedItemKinds.Contains(request.Kind) && request.AllowedSourceKinds.Contains(request.SourceKind) &&
            (request.RelatedSystemId is null || request.AllowedSystemIds.Contains(request.RelatedSystemId.Value)) &&
            (request.RelationshipType is null || request.AllowedRelationshipTypes.Contains(request.RelationshipType));
        var access = scoped ? accessPolicyEvaluator.Evaluate(new AccessRequest(
            request.Identity, request.Purpose, request.Action, request.ArchitectureItemId.ToString("D"),
            request.TenantId, request.Classification, request.RequiredRoles,
            ImmutableHashSet.Create(StringComparer.Ordinal, "developer.internal-service.architecture.discover"),
            request.SubjectId, RequiresDistinctApprover: false))
            : AccessDecision.Deny("existing_architecture_scope_denied", "Architecture item is outside OPA scope.");
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
            request.AuthorizationRequestId.ToString("D"), request.DiscoveryId.ToString("D"),
            request.ArchitectureItemId.ToString("D"), request.TenantId, request.SubjectId, request.Purpose,
            request.Action, request.SystemId, request.RelatedSystemId?.ToString() ?? string.Empty,
            request.Kind, request.RelationshipType ?? string.Empty, request.Classification, request.SourceKind,
            access.IsAllowed.ToString(CultureInfo.InvariantCulture), access.Code))));
        var evidence = request.EvidenceReferences
            .Append($"evidence://existing-architecture/authorization/{request.AuthorizationRequestId:D}/sha256/{digest}")
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        return Task.FromResult(new ExistingArchitectureResultAuthorizationDecision(
            request.AuthorizationRequestId, request.DiscoveryId, request.ArchitectureItemId,
            request.TenantId, request.Action, access.IsAllowed, access.Code, evidence, request.RequestedAt));
    }

    private static void Validate(ExistingArchitectureResultAuthorizationRequest request)
    {
        if (request.AuthorizationRequestId == Guid.Empty || request.DiscoveryId == Guid.Empty ||
            request.ArchitectureItemId == Guid.Empty || request.SystemId.Value == Guid.Empty ||
            request.Action != "existing-architecture.item.read" || request.RequestedAt == default ||
            request.RequiredRoles.IsEmpty || request.AllowedSystemIds.IsEmpty || request.AllowedItemKinds.IsEmpty ||
            request.AllowedRelationshipTypes.IsEmpty || request.AllowedSourceKinds.IsEmpty || request.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Existing Architecture result authorization request is incomplete.");
        ArgumentNullException.ThrowIfNull(request.Identity);
        if (!request.Identity.IsAuthenticated || !StringComparer.Ordinal.Equals(request.Identity.SubjectId, request.SubjectId) ||
            !StringComparer.Ordinal.Equals(request.Identity.TenantId, request.TenantId))
            throw new UnauthorizedAccessException("Existing Architecture result identity is mismatched.");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceKind);
    }
}
