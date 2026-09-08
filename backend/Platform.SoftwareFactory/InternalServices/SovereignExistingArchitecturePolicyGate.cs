using System.Collections.Immutable;
using System.Globalization;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;
using Platform.Governance.Policies;
using Platform.Integrations.ExistingArchitecture;

namespace Platform.SoftwareFactory.InternalService;

public sealed class SovereignExistingArchitecturePolicyGate(
    IPolicyBundleVerifier policyBundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : IExistingArchitecturePolicyGate
{
    public async Task<ExistingArchitecturePolicyDecision> EvaluateAsync(
        ExistingArchitecturePolicyInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!StringComparer.Ordinal.Equals(input.Action, "internal-service.existing-architecture.discover"))
            throw new UnauthorizedAccessException("Existing Architecture policy action is not authorized by this adapter.");

        var bundle = new SignedPolicyBundleReference(input.PolicyBundle.BundleId,
            input.PolicyBundle.Version, input.PolicyBundle.Sha256Digest,
            input.PolicyBundle.SignatureReference, input.PolicyBundle.Environment,
            input.PolicyBundle.ActivatedAt);
        var verification = await policyBundleVerifier.VerifyAsync(bundle, cancellationToken);
        var evaluatedAt = verification.VerifiedAt > input.EvaluatedAt
            ? verification.VerifiedAt
            : input.EvaluatedAt;
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["discoveryId"] = input.DiscoveryId.ToString("D"),
            ["systemsDiscoveryId"] = input.SystemsDiscoveryId.ToString("D"),
            ["contextDiscoveryId"] = input.ContextDiscoveryId.ToString("D"),
            ["registrationId"] = input.RegistrationId.ToString("D"),
            ["registrationVersion"] = input.RegistrationVersion.ToString(CultureInfo.InvariantCulture),
            ["intentSha256Digest"] = input.IntentSha256Digest,
            ["contextSha256Digest"] = input.ContextSha256Digest,
            ["inventorySha256Digest"] = input.InventorySha256Digest
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences.Append(verification.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(new SovereignPolicyEvaluationRequest(
            input.DecisionRequestId, input.Action, input.SystemsDiscoveryId.ToString("D"),
            input.TenantId, input.SubjectId, input.Purpose,
            input.MaximumClassification.ToString(), input.Environment, verification,
            attributes, evidence, evaluatedAt), cancellationToken);

        if (decision.Outcome == OpaDecisionOutcome.Deny)
        {
            if (decision.Scope is not null)
                throw new UnauthorizedAccessException("Denied Existing Architecture decision returned scope.");
            return CreateDecision(input, verification, decision,
                GovernedIntentPolicyOutcome.Deny, input.MaximumClassification,
                [], [], [], [], [], 0);
        }

        var envelope = decision.Scope
            ?? throw new UnauthorizedAccessException("OPA permit omitted Existing Architecture scope.");
        var scope = envelope.ExistingArchitecture
            ?? throw new UnauthorizedAccessException("OPA permit omitted action-specific Existing Architecture scope.");
        if (envelope.ExistingSystems is not null || envelope.ApprovedPackages is not null ||
            envelope.AiPlanning is not null || envelope.CodeGeneration is not null ||
            !envelope.AllowedResourceIds.IsDefaultOrEmpty ||
            !envelope.AllowedModalities.IsDefaultOrEmpty ||
            !envelope.RequiredRoles.IsDefaultOrEmpty || envelope.MaximumResults != 0 ||
            !string.IsNullOrWhiteSpace(envelope.MaximumClassification))
            throw new UnauthorizedAccessException("Existing Architecture decision mixed scopes from another action.");
        if (!Enum.TryParse<DataClassification>(scope.MaximumClassification, false,
                out var maximumClassification) || !Enum.IsDefined(maximumClassification) ||
            scope.AllowedSystemIds.IsDefaultOrEmpty || scope.AllowedItemKinds.IsDefaultOrEmpty ||
            scope.AllowedRelationshipTypes.IsDefaultOrEmpty || scope.RequiredRoles.IsDefaultOrEmpty ||
            scope.MaximumResults <= 0 ||
            !StringComparer.Ordinal.Equals(scope.AllowedSourceKind, "enterprise-graph"))
            throw new UnauthorizedAccessException("OPA returned invalid Existing Architecture scope.");

        var systemIds = Normalize(scope.AllowedSystemIds, "system identifier")
            .Select(value => Guid.TryParseExact(value, "D", out var id) && id != Guid.Empty
                ? new EnterpriseObjectId(id)
                : throw new UnauthorizedAccessException("OPA returned invalid architecture system identity."))
            .ToImmutableHashSet();
        var itemKinds = Normalize(scope.AllowedItemKinds, "item kind").Select(value =>
            Enum.TryParse<ExistingArchitectureItemKind>(value, false, out var kind) && Enum.IsDefined(kind)
                ? kind
                : throw new UnauthorizedAccessException("OPA returned invalid architecture item kind."))
            .ToImmutableHashSet();
        return CreateDecision(input, verification, decision,
            GovernedIntentPolicyOutcome.Permit, maximumClassification, systemIds,
            itemKinds, Normalize(scope.AllowedRelationshipTypes, "relationship type"),
            ImmutableHashSet.Create(StringComparer.Ordinal, scope.AllowedSourceKind),
            Normalize(scope.RequiredRoles, "required role"), scope.MaximumResults);
    }

    private static ExistingArchitecturePolicyDecision CreateDecision(
        ExistingArchitecturePolicyInput input, PolicyBundleVerification verification,
        SovereignPolicyEvaluationDecision decision, GovernedIntentPolicyOutcome outcome,
        DataClassification maximumClassification, ImmutableHashSet<EnterpriseObjectId> systemIds,
        ImmutableHashSet<ExistingArchitectureItemKind> itemKinds,
        ImmutableHashSet<string> relationshipTypes, ImmutableHashSet<string> sourceKinds,
        ImmutableHashSet<string> requiredRoles, int maximumResults) => new(
            decision.DecisionRequestId, input.DiscoveryId, input.SystemsDiscoveryId,
            input.ContextDiscoveryId, input.RegistrationId, input.RegistrationVersion,
            input.TenantId, input.IntentSha256Digest, input.ContextSha256Digest,
            input.InventorySha256Digest, decision.BundleId, decision.BundleVersion,
            decision.BundleSha256Digest, decision.Environment, PolicySignatureValid: true,
            verification.VerificationEvidenceReference, outcome, maximumClassification,
            systemIds, itemKinds, relationshipTypes, sourceKinds, requiredRoles, maximumResults,
            decision.Reasons, decision.EvidenceReferences, decision.DecidedAt);

    private static ImmutableHashSet<string> Normalize(ImmutableArray<string> values, string field)
    {
        foreach (var value in values) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var result = values.ToImmutableHashSet(StringComparer.Ordinal);
        if (result.Count != values.Length)
            throw new UnauthorizedAccessException($"OPA scope contains duplicate {field} values.");
        return result;
    }
}
