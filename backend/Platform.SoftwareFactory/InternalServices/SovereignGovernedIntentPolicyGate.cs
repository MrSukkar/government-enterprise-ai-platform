using System.Collections.Immutable;
using Platform.Governance.Policies;

namespace Platform.SoftwareFactory.InternalService;

public sealed class SovereignGovernedIntentPolicyGate(
    IPolicyBundleVerifier policyBundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : IGovernedIntentPolicyGate
{
    public async Task<GovernedIntentPolicyDecision> EvaluateAsync(
        GovernedIntentPolicyInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!StringComparer.Ordinal.Equals(input.Action, "internal-service.intent.register"))
            throw new UnauthorizedAccessException("Intent policy action is not authorized by this adapter.");

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
            ["registrationId"] = input.RegistrationId.ToString("D"),
            ["intentSha256Digest"] = input.IntentSha256Digest
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences
            .Append(verification.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(
            new SovereignPolicyEvaluationRequest(
                input.DecisionRequestId, input.Action, input.ResourceId, input.TenantId,
                input.SubjectId, input.Purpose, input.Classification.ToString(), input.Environment,
                verification, attributes, evidence, evaluatedAt),
            cancellationToken);

        return new GovernedIntentPolicyDecision(
            decision.DecisionRequestId, input.RegistrationId, input.TenantId,
            decision.BundleId, decision.BundleVersion, decision.BundleSha256Digest,
            decision.Environment, PolicySignatureValid: true,
            verification.VerificationEvidenceReference,
            decision.Outcome == OpaDecisionOutcome.Permit
                ? GovernedIntentPolicyOutcome.Permit
                : GovernedIntentPolicyOutcome.Deny,
            decision.Reasons, decision.EvidenceReferences, decision.DecidedAt);
    }
}
