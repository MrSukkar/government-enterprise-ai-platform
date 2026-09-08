using System.Collections.Immutable;
using System.Globalization;
using Platform.Domain.Security;
using Platform.Governance.Policies;

namespace Platform.SoftwareFactory.InternalService;

public sealed class SovereignStaticValidationPolicyGate(
    IPolicyBundleVerifier policyBundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : IStaticValidationPolicyGate
{
    public async Task<StaticValidationPolicyDecision> EvaluateAsync(StaticValidationPolicyInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!StringComparer.Ordinal.Equals(input.Environment, input.PolicyBundle.Environment))
            throw new UnauthorizedAccessException("Static Validation policy bundle environment does not match the request environment.");
        var bundle = new SignedPolicyBundleReference(input.PolicyBundle.BundleId, input.PolicyBundle.Version,
            input.PolicyBundle.Sha256Digest, input.PolicyBundle.SignatureReference,
            input.PolicyBundle.Environment, input.PolicyBundle.ActivatedAt);
        var verification = await policyBundleVerifier.VerifyAsync(bundle, cancellationToken);
        var evaluatedAt = verification.VerifiedAt > input.EvaluatedAt ? verification.VerifiedAt : input.EvaluatedAt;
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["validationId"] = input.ValidationId.ToString("D"), ["generationId"] = input.GenerationId.ToString("D"),
            ["deliveryRunId"] = input.DeliveryRunId.ToString("D"), ["candidateSha256Digest"] = input.CandidateSha256Digest,
            ["generationEvidenceReference"] = input.GenerationEvidenceReference,
            ["requiredControlCount"] = input.RequiredControlIds.Length.ToString(CultureInfo.InvariantCulture)
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences.Append(verification.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(new SovereignPolicyEvaluationRequest(input.DecisionRequestId,
            "internal-service.static-validation.create", input.GenerationId.ToString("D"), input.TenantId,
            input.SubjectId, input.Purpose, input.MaximumClassification.ToString(), input.Environment,
            verification, attributes, evidence, evaluatedAt), cancellationToken);
        if (decision.Outcome == OpaDecisionOutcome.Deny)
        {
            if (decision.Scope is not null) throw new UnauthorizedAccessException("Denied Static Validation decision returned scope.");
            return Create(input, verification, decision, GovernedIntentPolicyOutcome.Deny,
                input.MaximumClassification, [], [], string.Empty);
        }
        var envelope = decision.Scope ?? throw new UnauthorizedAccessException("OPA permit omitted Static Validation scope.");
        var scope = envelope.StaticValidation ?? throw new UnauthorizedAccessException("OPA permit omitted action-specific Static Validation scope.");
        if (envelope.ExistingSystems is not null || envelope.ExistingArchitecture is not null ||
            envelope.ApprovedPackages is not null || envelope.AiPlanning is not null || envelope.CodeGeneration is not null ||
            !envelope.AllowedResourceIds.IsDefaultOrEmpty || !envelope.AllowedModalities.IsDefaultOrEmpty ||
            !envelope.RequiredRoles.IsDefaultOrEmpty || envelope.MaximumResults != 0 ||
            !string.IsNullOrWhiteSpace(envelope.MaximumClassification))
            throw new UnauthorizedAccessException("Static Validation decision mixed scopes from another action.");
        if (!Enum.TryParse<DataClassification>(scope.MaximumClassification, false, out var classification) ||
            !Enum.IsDefined(classification) || scope.AllowedControlIds.IsDefaultOrEmpty ||
            scope.RequiredRoles.IsDefaultOrEmpty || !StringComparer.Ordinal.Equals(scope.OutputKind, "static-report"))
            throw new UnauthorizedAccessException("OPA returned invalid Static Validation scope.");
        var controls = Normalize(scope.AllowedControlIds); var roles = Normalize(scope.RequiredRoles);
        return Create(input, verification, decision, GovernedIntentPolicyOutcome.Permit,
            classification, controls, roles, scope.OutputKind);
    }

    private static ImmutableHashSet<string> Normalize(ImmutableArray<string> values)
    {
        if (values.Any(string.IsNullOrWhiteSpace)) throw new UnauthorizedAccessException("OPA returned an invalid Static set.");
        var result = values.ToImmutableHashSet(StringComparer.Ordinal);
        if (result.Count != values.Length) throw new UnauthorizedAccessException("OPA returned a duplicate Static set.");
        return result;
    }

    private static StaticValidationPolicyDecision Create(StaticValidationPolicyInput input,
        PolicyBundleVerification verification, SovereignPolicyEvaluationDecision decision,
        GovernedIntentPolicyOutcome outcome, DataClassification classification,
        ImmutableHashSet<string> controls, ImmutableHashSet<string> roles, string outputKind) => new(
            decision.DecisionRequestId, input.ValidationId, input.GenerationId, input.DeliveryRunId,
            input.TenantId, input.Environment, input.CandidateSha256Digest, decision.BundleId,
            decision.BundleVersion, decision.BundleSha256Digest, true,
            verification.VerificationEvidenceReference, outcome, classification, controls, roles,
            outputKind, decision.Reasons, decision.EvidenceReferences, decision.DecidedAt);
}
