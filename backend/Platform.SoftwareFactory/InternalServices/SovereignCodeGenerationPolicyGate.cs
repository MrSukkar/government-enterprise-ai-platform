using System.Collections.Immutable;
using System.Globalization;
using Platform.Domain.Security;
using Platform.Governance.Policies;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.InternalService;

public sealed class SovereignCodeGenerationPolicyGate(
    IPolicyBundleVerifier policyBundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : ICodeGenerationPolicyGate
{
    public async Task<CodeGenerationPolicyDecision> EvaluateAsync(
        CodeGenerationPolicyInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!StringComparer.Ordinal.Equals(input.Environment, input.PolicyBundle.Environment))
            throw new UnauthorizedAccessException("Code Generation policy environment is invalid.");
        var bundle = new SignedPolicyBundleReference(input.PolicyBundle.BundleId, input.PolicyBundle.Version,
            input.PolicyBundle.Sha256Digest, input.PolicyBundle.SignatureReference,
            input.PolicyBundle.Environment, input.PolicyBundle.ActivatedAt);
        var verification = await policyBundleVerifier.VerifyAsync(bundle, cancellationToken);
        var evaluatedAt = verification.VerifiedAt > input.EvaluatedAt ? verification.VerifiedAt : input.EvaluatedAt;
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["generationId"] = input.GenerationId.ToString("D"), ["planningId"] = input.PlanningId.ToString("D"),
            ["packageSelectionId"] = input.PackageSelectionId.ToString("D"), ["deliveryRunId"] = input.DeliveryRunId.ToString("D"),
            ["selectionSha256Digest"] = input.SelectionSha256Digest, ["planningSha256Digest"] = input.PlanningSha256Digest,
            ["promptTemplateId"] = input.PromptTemplateId, ["promptTemplateVersion"] = input.PromptTemplateVersion,
            ["runtimeProfile"] = input.RuntimeProfile,
            ["contextReferenceCount"] = input.ContextReferences.Length.ToString(CultureInfo.InvariantCulture),
            ["packageCount"] = input.ApprovedPackages.Length.ToString(CultureInfo.InvariantCulture),
            ["constraintCount"] = input.Constraints.Length.ToString(CultureInfo.InvariantCulture),
            ["outputPathCount"] = input.RequestedOutputPaths.Length.ToString(CultureInfo.InvariantCulture)
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences.Append(verification.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(new SovereignPolicyEvaluationRequest(
            input.DecisionRequestId, "internal-service.code-generation.create", input.PlanningId.ToString("D"),
            input.TenantId, input.SubjectId, input.Purpose, input.MaximumClassification.ToString(), input.Environment,
            verification, attributes, evidence, evaluatedAt), cancellationToken);
        if (decision.Outcome == OpaDecisionOutcome.Deny)
        {
            if (decision.Scope is not null) throw new UnauthorizedAccessException("Denied Code Generation decision returned scope.");
            return Create(input, verification, decision, GovernedIntentPolicyOutcome.Deny, input.MaximumClassification,
                string.Empty, string.Empty, string.Empty, string.Empty, [], [], [], [], [], string.Empty, 0, 0, 0);
        }

        var envelope = decision.Scope ?? throw new UnauthorizedAccessException("OPA permit omitted Code Generation scope.");
        var scope = envelope.CodeGeneration ?? throw new UnauthorizedAccessException("OPA permit omitted action-specific Code Generation scope.");
        if (envelope.ExistingSystems is not null || envelope.ExistingArchitecture is not null ||
            envelope.ApprovedPackages is not null || envelope.AiPlanning is not null || envelope.StaticValidation is not null ||
            envelope.SecurityValidation is not null || envelope.Sandbox is not null ||
            !envelope.AllowedResourceIds.IsDefaultOrEmpty || !envelope.AllowedModalities.IsDefaultOrEmpty ||
            !envelope.RequiredRoles.IsDefaultOrEmpty || envelope.MaximumResults != 0 ||
            !string.IsNullOrWhiteSpace(envelope.MaximumClassification))
            throw new UnauthorizedAccessException("Code Generation decision mixed scopes from another action.");
        if (!Enum.TryParse<DataClassification>(scope.MaximumClassification, false, out var classification) ||
            !Enum.IsDefined(classification) || string.IsNullOrWhiteSpace(scope.AllowedPromptTemplateId) ||
            string.IsNullOrWhiteSpace(scope.AllowedPromptTemplateVersion) || !ValidDigest(scope.AllowedPromptSha256Digest) ||
            string.IsNullOrWhiteSpace(scope.AllowedRuntimeProfile) || scope.AllowedContextReferences.IsDefaultOrEmpty ||
            scope.AllowedPackages.IsDefaultOrEmpty || scope.AllowedOutputPaths.IsDefaultOrEmpty ||
            scope.RequiredRoles.IsDefaultOrEmpty || !StringComparer.Ordinal.Equals(scope.OutputKind, "inert-code-candidate") ||
            scope.RequestTimeoutSeconds <= 0 || scope.MaximumRequestBytes <= 0 || scope.MaximumResponseBytes <= 0)
            throw new UnauthorizedAccessException("OPA returned invalid Code Generation scope.");
        var contexts = Normalize(scope.AllowedContextReferences, "context references");
        var constraints = Normalize(scope.AllowedConstraints, "constraints");
        var paths = Normalize(scope.AllowedOutputPaths, "output paths");
        foreach (var path in paths) GovernedGeneratedPath.Validate(path);
        var roles = Normalize(scope.RequiredRoles, "required roles");
        var packages = scope.AllowedPackages.Select(MapCoordinate).ToImmutableHashSet();
        if (packages.Count != scope.AllowedPackages.Length) throw new UnauthorizedAccessException("OPA returned duplicate package coordinates.");
        return Create(input, verification, decision, GovernedIntentPolicyOutcome.Permit, classification,
            scope.AllowedPromptTemplateId, scope.AllowedPromptTemplateVersion, scope.AllowedPromptSha256Digest.ToLowerInvariant(),
            scope.AllowedRuntimeProfile, contexts, packages, constraints, paths, roles, scope.OutputKind,
            scope.RequestTimeoutSeconds, scope.MaximumRequestBytes, scope.MaximumResponseBytes);
    }

    private static PackageCoordinate MapCoordinate(SovereignApprovedPackageCoordinate value)
    {
        if (!Enum.TryParse<PackageKind>(value.Kind, false, out var kind) || !Enum.IsDefined(kind))
            throw new UnauthorizedAccessException("OPA returned invalid Code Generation package kind.");
        var coordinate = new PackageCoordinate(kind, value.Name, value.Version, value.ContentDigest);
        GovernedApprovedPackagesSelectionRequest.ValidateExactCoordinate(coordinate); return coordinate;
    }

    private static ImmutableHashSet<string> Normalize(ImmutableArray<string> values, string owner)
    {
        if (values.IsDefault || values.Any(string.IsNullOrWhiteSpace)) throw new UnauthorizedAccessException($"OPA returned invalid {owner}.");
        var result = values.ToImmutableHashSet(StringComparer.Ordinal);
        if (result.Count != values.Length) throw new UnauthorizedAccessException($"OPA returned duplicate {owner}.");
        return result;
    }

    private static bool ValidDigest(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);

    private static CodeGenerationPolicyDecision Create(CodeGenerationPolicyInput input,
        PolicyBundleVerification verification, SovereignPolicyEvaluationDecision decision,
        GovernedIntentPolicyOutcome outcome, DataClassification classification, string promptId,
        string promptVersion, string promptDigest, string runtimeProfile, ImmutableHashSet<string> contexts,
        ImmutableHashSet<PackageCoordinate> packages, ImmutableHashSet<string> constraints,
        ImmutableHashSet<string> paths, ImmutableHashSet<string> roles, string outputKind,
        int timeout, int maximumRequestBytes, int maximumResponseBytes) => new(
            decision.DecisionRequestId, input.GenerationId, input.PlanningId, input.PackageSelectionId,
            input.DeliveryRunId, input.TenantId, input.Environment, input.SelectionSha256Digest,
            input.PlanningSha256Digest, decision.BundleId, decision.BundleVersion, decision.BundleSha256Digest,
            true, verification.VerificationEvidenceReference, outcome, classification, promptId, promptVersion,
            promptDigest, runtimeProfile, contexts, packages, constraints, paths, roles, outputKind,
            timeout, maximumRequestBytes, maximumResponseBytes, decision.Reasons,
            decision.EvidenceReferences, decision.DecidedAt);
}
