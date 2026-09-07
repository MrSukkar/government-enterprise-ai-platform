using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Identity.Access;

namespace Platform.SoftwareFactory.InternalService;

public sealed class DeterministicExistingSystemResultAuthorizer(
    IAccessPolicyEvaluator accessPolicyEvaluator) : IExistingSystemResultAuthorizer
{
    public Task<ExistingSystemResultAuthorizationDecision> AuthorizeAsync(
        ExistingSystemResultAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        Validate(request);

        var scopeAllowed = request.AllowedSystemIds.Contains(request.SystemId) &&
            request.AllowedSourceKinds.Contains(request.SourceKind) &&
            (request.RelatedSystemId is null ||
             request.AllowedSystemIds.Contains(request.RelatedSystemId.Value)) &&
            (request.RelationshipType is null ||
             request.AllowedRelationshipTypes.Contains(request.RelationshipType));
        var access = scopeAllowed
            ? accessPolicyEvaluator.Evaluate(new AccessRequest(
                request.Identity, request.Purpose, request.Action,
                request.RelatedSystemId is null
                    ? request.SystemId.ToString()
                    : $"{request.SystemId}/{request.RelationshipType}/{request.RelatedSystemId}",
                request.TenantId, request.Classification, request.RequiredRoles,
                ImmutableHashSet.Create(StringComparer.Ordinal,
                    "developer.internal-service.systems.discover"),
                request.SubjectId, RequiresDistinctApprover: false))
            : AccessDecision.Deny("existing_system_scope_denied",
                "The system or relationship is outside the OPA-authorized scope.");
        var decidedAt = request.RequestedAt;
        var digest = Digest(request, access);
        var evidence = request.EvidenceReferences
            .Append($"evidence://existing-systems/authorization/{request.AuthorizationRequestId:D}/sha256/{digest}")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        return Task.FromResult(new ExistingSystemResultAuthorizationDecision(
            request.AuthorizationRequestId, request.DiscoveryId, request.TenantId,
            request.Action, request.SystemId, request.RelatedSystemId,
            request.RelationshipType, access.IsAllowed, access.Code, evidence, decidedAt));
    }

    private static void Validate(ExistingSystemResultAuthorizationRequest request)
    {
        if (request.AuthorizationRequestId == Guid.Empty || request.DiscoveryId == Guid.Empty)
            throw new InvalidOperationException("Existing Systems result authorization identity is invalid.");
        ArgumentNullException.ThrowIfNull(request.Identity);
        if (!request.Identity.IsAuthenticated ||
            !StringComparer.Ordinal.Equals(request.Identity.SubjectId, request.SubjectId) ||
            !StringComparer.Ordinal.Equals(request.Identity.TenantId, request.TenantId))
            throw new UnauthorizedAccessException("Existing Systems result identity is mismatched.");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Action);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceKind);
        if (request.Action is not ("existing-system.read" or "existing-system.relationship.read") ||
            request.SystemId.Value == Guid.Empty || !Enum.IsDefined(request.Classification) ||
            request.RequestedAt == default || request.RequiredRoles.IsEmpty ||
            request.AllowedSystemIds.IsEmpty || request.AllowedRelationshipTypes.IsEmpty ||
            request.AllowedSourceKinds.IsEmpty || request.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Existing Systems result authorization request is incomplete.");
        if (request.Action == "existing-system.relationship.read" &&
            (request.RelatedSystemId is null || request.RelatedSystemId.Value.Value == Guid.Empty ||
             string.IsNullOrWhiteSpace(request.RelationshipType)))
            throw new InvalidOperationException("Relationship authorization requires an exact relationship scope.");
        foreach (var value in request.RequiredRoles.Concat(request.AllowedRelationshipTypes)
                     .Concat(request.AllowedSourceKinds).Concat(request.EvidenceReferences))
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
    }

    private static string Digest(
        ExistingSystemResultAuthorizationRequest request,
        AccessDecision decision)
    {
        var canonical = string.Join('\u001f', request.AuthorizationRequestId.ToString("D"),
            request.DiscoveryId.ToString("D"), request.TenantId, request.SubjectId,
            request.Purpose, request.Action, request.SystemId.ToString(),
            request.RelatedSystemId?.ToString() ?? string.Empty,
            request.RelationshipType ?? string.Empty, request.Classification,
            request.SourceKind, string.Join('\u001e', request.RequiredRoles.Order(StringComparer.Ordinal)),
            string.Join('\u001e', request.AllowedSystemIds.Select(id => id.ToString()).Order(StringComparer.Ordinal)),
            string.Join('\u001e', request.AllowedRelationshipTypes.Order(StringComparer.Ordinal)),
            string.Join('\u001e', request.AllowedSourceKinds.Order(StringComparer.Ordinal)),
            decision.IsAllowed.ToString(CultureInfo.InvariantCulture), decision.Code);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
