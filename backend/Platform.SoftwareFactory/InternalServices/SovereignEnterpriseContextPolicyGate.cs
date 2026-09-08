using System.Collections.Immutable;
using System.Globalization;
using Platform.Domain.Security;
using Platform.Governance.Policies;
using Platform.Knowledge.Retrieval;

namespace Platform.SoftwareFactory.InternalService;

public sealed class SovereignEnterpriseContextPolicyGate(
    IPolicyBundleVerifier policyBundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : IEnterpriseContextPolicyGate
{
    public async Task<EnterpriseContextPolicyDecision> EvaluateAsync(
        EnterpriseContextPolicyInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!StringComparer.Ordinal.Equals(input.Action, "internal-service.enterprise-context.discover"))
            throw new UnauthorizedAccessException("Enterprise Context policy action is not authorized by this adapter.");

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
            ["registrationId"] = input.RegistrationId.ToString("D"),
            ["registrationVersion"] = input.RegistrationVersion.ToString(CultureInfo.InvariantCulture),
            ["intentSha256Digest"] = input.IntentSha256Digest
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences
            .Append(verification.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(
            new SovereignPolicyEvaluationRequest(
                input.DecisionRequestId, input.Action, input.RegistrationId.ToString("D"),
                input.TenantId, input.SubjectId, input.Purpose,
                input.RegistrationClassification.ToString(), input.Environment,
                verification, attributes, evidence, evaluatedAt),
            cancellationToken);

        if (decision.Outcome == OpaDecisionOutcome.Deny)
        {
            if (decision.Scope is not null)
                throw new UnauthorizedAccessException("Denied Enterprise Context policy decision returned a scope.");
            return CreateDecision(input, verification, decision,
                GovernedIntentPolicyOutcome.Deny, input.RegistrationClassification,
                [], [], [], 0);
        }

        var scope = decision.Scope
            ?? throw new UnauthorizedAccessException("OPA permit did not return Enterprise Context scope.");
        if (scope.ExistingSystems is not null || scope.ExistingArchitecture is not null ||
            scope.ApprovedPackages is not null || scope.AiPlanning is not null || scope.CodeGeneration is not null || scope.StaticValidation is not null ||
            !Enum.TryParse<DataClassification>(scope.MaximumClassification, ignoreCase: false,
                out var maximumClassification) || !Enum.IsDefined(maximumClassification) ||
            scope.AllowedResourceIds.IsDefaultOrEmpty || scope.AllowedModalities.IsDefaultOrEmpty ||
            scope.RequiredRoles.IsDefault || scope.MaximumResults <= 0)
            throw new UnauthorizedAccessException("OPA returned invalid Enterprise Context scope.");
        var resourceIds = Normalize(scope.AllowedResourceIds);
        var requiredRoles = Normalize(scope.RequiredRoles);
        var modalities = scope.AllowedModalities.Select(value =>
        {
            if (!Enum.TryParse<RetrievalModality>(value, ignoreCase: false, out var modality) ||
                !Enum.IsDefined(modality))
                throw new UnauthorizedAccessException("OPA returned an invalid retrieval modality.");
            return modality;
        }).ToImmutableHashSet();
        if (modalities.Count != 1 || !modalities.Contains(RetrievalModality.Graph))
            throw new UnauthorizedAccessException("This wave permits only Graph Enterprise Context retrieval.");

        return CreateDecision(input, verification, decision,
            GovernedIntentPolicyOutcome.Permit, maximumClassification,
            resourceIds, modalities, requiredRoles, scope.MaximumResults);
    }

    private static EnterpriseContextPolicyDecision CreateDecision(
        EnterpriseContextPolicyInput input,
        PolicyBundleVerification verification,
        SovereignPolicyEvaluationDecision decision,
        GovernedIntentPolicyOutcome outcome,
        DataClassification maximumClassification,
        ImmutableHashSet<string> resourceIds,
        ImmutableHashSet<RetrievalModality> modalities,
        ImmutableHashSet<string> requiredRoles,
        int maximumResults) =>
        new(
            decision.DecisionRequestId, input.DiscoveryId, input.RegistrationId,
            input.RegistrationVersion, input.TenantId, decision.BundleId,
            decision.BundleVersion, decision.BundleSha256Digest, decision.Environment,
            PolicySignatureValid: true, verification.VerificationEvidenceReference,
            outcome, maximumClassification, resourceIds, modalities, requiredRoles,
            maximumResults, decision.Reasons, decision.EvidenceReferences, decision.DecidedAt);

    private static ImmutableHashSet<string> Normalize(ImmutableArray<string> values)
    {
        foreach (var value in values) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = values.ToImmutableHashSet(StringComparer.Ordinal);
        if (normalized.Count != values.Length)
            throw new UnauthorizedAccessException("OPA Enterprise Context scope contains duplicates.");
        return normalized;
    }
}
