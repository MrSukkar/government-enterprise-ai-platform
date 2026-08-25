using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Registration;

public sealed class AutomaticRegistrationEngine(IAutomaticRegistrationRepository repository)
{
    public async Task<AutomaticRegistrationCommit> RegisterAsync(
        AutomaticRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var evidence = BuildEvidence(request);
        var fingerprint = BuildFingerprint(request, evidence);
        var candidate = BuildCandidate(request, evidence);
        var proposal = new AutomaticRegistrationProposal(
            request.Id,
            request.Key,
            fingerprint,
            candidate,
            evidence,
            request.RegisteredAt);

        var commit = await repository.RegisterAtomicallyAsync(proposal, cancellationToken);
        ValidateCommit(request, fingerprint, commit);
        return commit;
    }

    private static EnterpriseObject BuildCandidate(
        AutomaticRegistrationRequest request,
        ImmutableArray<string> evidence)
    {
        var relationships = request.Relationships
            .Select(relationship => new EnterpriseRelationship(
                relationship.TargetId,
                relationship.RelationshipType,
                RelationshipKnowledgeState.Confirmed,
                Confidence: 1m,
                Source: "automatic-registration",
                relationship.EvidenceReferences,
                request.RegisteredAt))
            .ToImmutableArray();

        return new EnterpriseObject
        {
            Id = EnterpriseObjectId.New(),
            TenantId = request.Key.TenantId,
            Type = request.EnterpriseObjectType,
            State = "registered",
            OwnerId = request.OwnerId,
            Classification = request.Classification,
            Relationships = relationships,
            PolicyReferences = Normalize(request.PolicyReferences),
            PermittedActions = Normalize(request.PermittedActions),
            Source = "automatic-registration",
            Confidence = 1m,
            EvidenceReferences = evidence,
            Lifecycle = LifecycleState.Active,
            CreatedAt = request.RegisteredAt,
            UpdatedAt = request.RegisteredAt
        }.Validate();
    }

    private static ImmutableArray<string> BuildEvidence(AutomaticRegistrationRequest request) =>
        Normalize(request.EvidenceReferences.AddRange(ImmutableArray.Create(
            request.ArtifactDigest,
            request.RegistryReference,
            request.DeploymentEvidenceReference,
            request.SupplyChainEvidenceReference,
            request.ObservabilityEvidenceReference,
            request.HumanApprovalReference
        )));

    private static ImmutableArray<string> Normalize(ImmutableArray<string> values) =>
        values.Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();

    private static string BuildFingerprint(
        AutomaticRegistrationRequest request,
        ImmutableArray<string> evidence)
    {
        var canonical = new StringBuilder();
        Append(canonical, request.Key.TenantId);
        Append(canonical, request.Key.EnvironmentName);
        Append(canonical, request.Key.ServiceIdentity);
        Append(canonical, request.EnterpriseObjectType);
        Append(canonical, request.OwnerId);
        Append(canonical, request.Classification.ToString());
        Append(canonical, request.ArtifactDigest.ToLowerInvariant());
        foreach (var value in evidence) Append(canonical, value);
        foreach (var value in Normalize(request.PolicyReferences)) Append(canonical, value);
        foreach (var value in Normalize(request.PermittedActions)) Append(canonical, value);
        foreach (var relationship in request.Relationships
                     .OrderBy(item => item.TargetId.ToString(), StringComparer.Ordinal)
                     .ThenBy(item => item.RelationshipType, StringComparer.Ordinal))
        {
            Append(canonical, relationship.TargetId.ToString());
            Append(canonical, relationship.RelationshipType);
            foreach (var value in Normalize(relationship.EvidenceReferences)) Append(canonical, value);
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return $"sha256:{Convert.ToHexStringLower(digest)}";
    }

    private static void Append(StringBuilder builder, string value) =>
        builder.Append(value.Length).Append(':').Append(value).Append('|');

    private static void ValidateCommit(
        AutomaticRegistrationRequest request,
        string fingerprint,
        AutomaticRegistrationCommit commit)
    {
        ArgumentNullException.ThrowIfNull(commit);
        if (!Enum.IsDefined<RegistrationDisposition>(commit.Disposition))
            throw new InvalidOperationException("Registration repository returned an invalid disposition.");
        if (commit.RequestId != request.Id || commit.Key != request.Key ||
            !StringComparer.Ordinal.Equals(commit.RequestFingerprint, fingerprint))
            throw new InvalidOperationException("Registration repository returned mismatched idempotency evidence.");
        commit.EnterpriseObject.Validate();
        if (!StringComparer.Ordinal.Equals(commit.EnterpriseObject.TenantId, request.Key.TenantId) ||
            !StringComparer.Ordinal.Equals(commit.EnterpriseObject.Source, "automatic-registration") ||
            !StringComparer.Ordinal.Equals(commit.EnterpriseObject.Type, request.EnterpriseObjectType) ||
            !StringComparer.Ordinal.Equals(commit.EnterpriseObject.OwnerId, request.OwnerId) ||
            commit.EnterpriseObject.Classification != request.Classification ||
            !StringComparer.Ordinal.Equals(commit.EnterpriseObject.State, "registered") ||
            commit.EnterpriseObject.Lifecycle != LifecycleState.Active)
            throw new InvalidOperationException("Registration repository returned an invalid Enterprise Object scope.");
        if (!commit.EnterpriseObject.PolicyReferences.ToHashSet(StringComparer.Ordinal)
                .SetEquals(Normalize(request.PolicyReferences)) ||
            !commit.EnterpriseObject.PermittedActions.ToHashSet(StringComparer.Ordinal)
                .SetEquals(Normalize(request.PermittedActions)) ||
            !BuildEvidence(request).All(commit.EnterpriseObject.EvidenceReferences.Contains))
            throw new InvalidOperationException("Registration repository changed governed policies, actions, or evidence.");
        if (commit.EnterpriseObject.Relationships.Length != request.Relationships.Length ||
            request.Relationships.Any(expected => !commit.EnterpriseObject.Relationships.Any(actual =>
                actual.TargetId == expected.TargetId &&
                StringComparer.Ordinal.Equals(actual.RelationshipType, expected.RelationshipType) &&
                actual.KnowledgeState == RelationshipKnowledgeState.Confirmed &&
                expected.EvidenceReferences.All(actual.EvidenceReferences.Contains))))
            throw new InvalidOperationException("Registration repository changed governed relationships.");
        ArgumentException.ThrowIfNullOrWhiteSpace(commit.EvidenceReference);
        if (commit.CommittedAt < request.RegisteredAt)
            throw new InvalidOperationException("Registration commit cannot precede the request.");
    }
}
