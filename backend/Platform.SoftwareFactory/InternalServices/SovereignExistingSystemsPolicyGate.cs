using System.Collections.Immutable;
using System.Globalization;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;
using Platform.Governance.Policies;

namespace Platform.SoftwareFactory.InternalService;

public sealed class SovereignExistingSystemsPolicyGate(
    IPolicyBundleVerifier policyBundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : IExistingSystemsPolicyGate
{
    public async Task<ExistingSystemsPolicyDecision> EvaluateAsync(
        ExistingSystemsPolicyInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!StringComparer.Ordinal.Equals(input.Action, "internal-service.existing-systems.discover"))
            throw new UnauthorizedAccessException("Existing Systems policy action is not authorized by this adapter.");

        var bundle = new SignedPolicyBundleReference(
            input.PolicyBundle.BundleId, input.PolicyBundle.Version,
            input.PolicyBundle.Sha256Digest, input.PolicyBundle.SignatureReference,
            input.PolicyBundle.Environment, input.PolicyBundle.ActivatedAt);
        var verification = await policyBundleVerifier.VerifyAsync(bundle, cancellationToken);
        var evaluatedAt = verification.VerifiedAt > input.EvaluatedAt
            ? verification.VerifiedAt
            : input.EvaluatedAt;
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["discoveryId"] = input.DiscoveryId.ToString("D"),
            ["contextDiscoveryId"] = input.ContextDiscoveryId.ToString("D"),
            ["registrationId"] = input.RegistrationId.ToString("D"),
            ["registrationVersion"] = input.RegistrationVersion.ToString(CultureInfo.InvariantCulture),
            ["intentSha256Digest"] = input.IntentSha256Digest,
            ["contextSha256Digest"] = input.ContextSha256Digest
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences
            .Append(verification.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(
            new SovereignPolicyEvaluationRequest(
                input.DecisionRequestId, input.Action, input.ContextDiscoveryId.ToString("D"),
                input.TenantId, input.SubjectId, input.Purpose,
                input.MaximumClassification.ToString(), input.Environment,
                verification, attributes, evidence, evaluatedAt),
            cancellationToken);

        if (decision.Outcome == OpaDecisionOutcome.Deny)
        {
            if (decision.Scope is not null)
                throw new UnauthorizedAccessException("Denied Existing Systems policy decision returned a scope.");
            return CreateDecision(input, verification, decision, GovernedIntentPolicyOutcome.Deny,
                input.MaximumClassification, [], [], [], [], 0);
        }

        var envelope = decision.Scope
            ?? throw new UnauthorizedAccessException("OPA permit did not return an Existing Systems scope envelope.");
        var scope = envelope.ExistingSystems
            ?? throw new UnauthorizedAccessException("OPA permit did not return the action-specific Existing Systems scope.");
        if (envelope.ExistingArchitecture is not null || envelope.ApprovedPackages is not null ||
            envelope.AiPlanning is not null || envelope.CodeGeneration is not null ||
            !envelope.AllowedResourceIds.IsDefaultOrEmpty ||
            !envelope.AllowedModalities.IsDefaultOrEmpty ||
            !envelope.RequiredRoles.IsDefaultOrEmpty || envelope.MaximumResults != 0 ||
            !string.IsNullOrWhiteSpace(envelope.MaximumClassification))
            throw new UnauthorizedAccessException("Existing Systems policy decision mixed scopes from another action.");
        if (!Enum.TryParse<DataClassification>(scope.MaximumClassification, ignoreCase: false,
                out var maximumClassification) || !Enum.IsDefined(maximumClassification) ||
            scope.AllowedSystemIds.IsDefaultOrEmpty || scope.AllowedRelationshipTypes.IsDefaultOrEmpty ||
            scope.RequiredRoles.IsDefaultOrEmpty || scope.MaximumResults <= 0 ||
            !StringComparer.Ordinal.Equals(scope.AllowedSourceKind, "enterprise-graph"))
            throw new UnauthorizedAccessException("OPA returned an invalid Existing Systems scope.");

        var systemIds = Normalize(scope.AllowedSystemIds, "system identifier")
            .Select(value => Guid.TryParseExact(value, "D", out var parsed) && parsed != Guid.Empty
                ? new EnterpriseObjectId(parsed)
                : throw new UnauthorizedAccessException("OPA returned an invalid Existing Systems identifier."))
            .ToImmutableHashSet();
        return CreateDecision(input, verification, decision, GovernedIntentPolicyOutcome.Permit,
            maximumClassification, systemIds,
            Normalize(scope.AllowedRelationshipTypes, "relationship type"),
            ImmutableHashSet.Create(StringComparer.Ordinal, scope.AllowedSourceKind),
            Normalize(scope.RequiredRoles, "required role"), scope.MaximumResults);
    }

    private static ExistingSystemsPolicyDecision CreateDecision(
        ExistingSystemsPolicyInput input,
        PolicyBundleVerification verification,
        SovereignPolicyEvaluationDecision decision,
        GovernedIntentPolicyOutcome outcome,
        DataClassification maximumClassification,
        ImmutableHashSet<EnterpriseObjectId> systemIds,
        ImmutableHashSet<string> relationshipTypes,
        ImmutableHashSet<string> sourceKinds,
        ImmutableHashSet<string> requiredRoles,
        int maximumResults) =>
        new(decision.DecisionRequestId, input.DiscoveryId, input.ContextDiscoveryId,
            input.RegistrationId, input.RegistrationVersion, input.TenantId,
            decision.BundleId, decision.BundleVersion, decision.BundleSha256Digest,
            decision.Environment, PolicySignatureValid: true,
            verification.VerificationEvidenceReference, outcome, maximumClassification,
            systemIds, relationshipTypes, sourceKinds, requiredRoles, maximumResults,
            decision.Reasons, decision.EvidenceReferences, decision.DecidedAt);

    private static ImmutableHashSet<string> Normalize(ImmutableArray<string> values, string field)
    {
        foreach (var value in values) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = values.ToImmutableHashSet(StringComparer.Ordinal);
        if (normalized.Count != values.Length)
            throw new UnauthorizedAccessException($"OPA Existing Systems scope contains duplicate {field} values.");
        return normalized;
    }
}
