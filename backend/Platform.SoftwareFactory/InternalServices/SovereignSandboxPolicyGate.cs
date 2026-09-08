using System.Collections.Immutable;
using System.Globalization;
using Platform.Domain.Security;
using Platform.Governance.Policies;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.InternalService;

public sealed class SovereignSandboxPolicyGate(
    IPolicyBundleVerifier policyBundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : ISandboxPolicyGate
{
    public async Task<SandboxPolicyDecision> EvaluateAsync(
        SandboxPolicyInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!StringComparer.Ordinal.Equals(input.Environment, input.PolicyBundle.Environment))
            throw new UnauthorizedAccessException("Sandbox policy bundle environment does not match the request environment.");
        var bundle = new SignedPolicyBundleReference(input.PolicyBundle.BundleId, input.PolicyBundle.Version,
            input.PolicyBundle.Sha256Digest, input.PolicyBundle.SignatureReference,
            input.PolicyBundle.Environment, input.PolicyBundle.ActivatedAt);
        var verification = await policyBundleVerifier.VerifyAsync(bundle, cancellationToken);
        var evaluatedAt = verification.VerifiedAt > input.EvaluatedAt ? verification.VerifiedAt : input.EvaluatedAt;
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["executionId"] = input.ExecutionId.ToString("D"),
            ["securityValidationId"] = input.SecurityValidationId.ToString("D"),
            ["generationId"] = input.GenerationId.ToString("D"),
            ["deliveryRunId"] = input.DeliveryRunId.ToString("D"),
            ["candidateSha256Digest"] = input.CandidateSha256Digest,
            ["securityReportSha256Digest"] = input.SecurityReportSha256Digest,
            ["securityEvidenceReference"] = input.SecurityEvidenceReference,
            ["sandboxImage"] = CoordinateKey(input.SandboxImage),
            ["isolationClass"] = input.IsolationPolicy.IsolationClass,
            ["ephemeral"] = input.IsolationPolicy.Ephemeral.ToString(CultureInfo.InvariantCulture),
            ["microVmIsolation"] = input.IsolationPolicy.MicroVmIsolation.ToString(CultureInfo.InvariantCulture),
            ["productionCredentialsAllowed"] = input.IsolationPolicy.ProductionCredentialsAllowed.ToString(CultureInfo.InvariantCulture),
            ["hostFilesystemAccessAllowed"] = input.IsolationPolicy.HostFilesystemAccessAllowed.ToString(CultureInfo.InvariantCulture),
            ["networkDefaultDeny"] = input.IsolationPolicy.NetworkDefaultDeny.ToString(CultureInfo.InvariantCulture),
            ["cpuLimit"] = input.IsolationPolicy.CpuLimit.ToString(CultureInfo.InvariantCulture),
            ["memoryLimitBytes"] = input.IsolationPolicy.MemoryLimitBytes.ToString(CultureInfo.InvariantCulture),
            ["executionTimeoutTicks"] = input.IsolationPolicy.ExecutionTimeout.Ticks.ToString(CultureInfo.InvariantCulture),
            ["environmentReferences"] = string.Join(',', input.NonSecretEnvironmentReferences
                .OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => $"{item.Key}={item.Value}")),
            ["networkDestinations"] = string.Join(',', input.IsolationPolicy.AllowedNetworkDestinations.Order(StringComparer.Ordinal))
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences.Append(verification.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(new SovereignPolicyEvaluationRequest(
            input.DecisionRequestId, "internal-service.sandbox.execute", input.SecurityValidationId.ToString("D"),
            input.TenantId, input.SubjectId, input.Purpose, input.MaximumClassification.ToString(),
            input.Environment, verification, attributes, evidence, evaluatedAt), cancellationToken);
        if (decision.Outcome == OpaDecisionOutcome.Deny)
        {
            if (decision.Scope is not null) throw new UnauthorizedAccessException("Denied Sandbox decision returned scope.");
            return Create(input, verification, decision, GovernedIntentPolicyOutcome.Deny,
                input.MaximumClassification, [], [], [], string.Empty);
        }
        var envelope = decision.Scope ?? throw new UnauthorizedAccessException("OPA permit omitted Sandbox scope.");
        var scope = envelope.Sandbox ?? throw new UnauthorizedAccessException("OPA permit omitted action-specific Sandbox scope.");
        if (envelope.ExistingSystems is not null || envelope.ExistingArchitecture is not null ||
            envelope.ApprovedPackages is not null || envelope.AiPlanning is not null || envelope.CodeGeneration is not null ||
            envelope.StaticValidation is not null || envelope.SecurityValidation is not null || envelope.Tests is not null || envelope.HumanReview is not null ||
            !envelope.AllowedResourceIds.IsDefaultOrEmpty || !envelope.AllowedModalities.IsDefaultOrEmpty ||
            !envelope.RequiredRoles.IsDefaultOrEmpty || envelope.MaximumResults != 0 ||
            !string.IsNullOrWhiteSpace(envelope.MaximumClassification))
            throw new UnauthorizedAccessException("Sandbox decision mixed scopes from another action.");
        if (!Enum.TryParse<DataClassification>(scope.MaximumClassification, false, out var classification) ||
            !Enum.IsDefined(classification) || scope.AllowedSandboxImage is null ||
            scope.AllowedEnvironmentReferences is null || scope.AllowedNetworkDestinations.IsDefault ||
            scope.RequiredRoles.IsDefaultOrEmpty || !StringComparer.Ordinal.Equals(scope.OutputKind, "sandbox-result"))
            throw new UnauthorizedAccessException("OPA returned invalid Sandbox scope.");
        var image = ToCoordinate(scope.AllowedSandboxImage);
        var isolation = new Sandbox.SandboxIsolationPolicy(scope.IsolationClass, scope.Ephemeral,
            scope.MicroVmIsolation, scope.ProductionCredentialsAllowed, scope.HostFilesystemAccessAllowed,
            scope.NetworkDefaultDeny, Normalize(scope.AllowedNetworkDestinations), scope.CpuLimit,
            scope.MemoryLimitBytes, TimeSpan.FromTicks(scope.ExecutionTimeoutTicks));
        isolation.Validate();
        if (image != input.SandboxImage || !IsolationMatches(isolation, input.IsolationPolicy) ||
            scope.AllowedEnvironmentReferences.Count != input.NonSecretEnvironmentReferences.Count ||
            scope.AllowedEnvironmentReferences.Any(item => !input.NonSecretEnvironmentReferences.TryGetValue(item.Key, out var value) ||
                !StringComparer.Ordinal.Equals(item.Value, value)))
            throw new UnauthorizedAccessException("OPA Sandbox scope does not exactly match the governed request.");
        return Create(input, verification, decision, GovernedIntentPolicyOutcome.Permit, classification,
            scope.AllowedEnvironmentReferences.ToImmutableDictionary(StringComparer.Ordinal),
            Normalize(scope.AllowedNetworkDestinations), Normalize(scope.RequiredRoles), scope.OutputKind);
    }

    private static SandboxPolicyDecision Create(SandboxPolicyInput input, PolicyBundleVerification verification,
        SovereignPolicyEvaluationDecision decision, GovernedIntentPolicyOutcome outcome,
        DataClassification classification, ImmutableDictionary<string, string> environmentReferences,
        ImmutableHashSet<string> networkDestinations, ImmutableHashSet<string> roles, string outputKind) => new(
            decision.DecisionRequestId, input.ExecutionId, input.SecurityValidationId, input.GenerationId,
            input.DeliveryRunId, input.TenantId, input.Environment, input.CandidateSha256Digest,
            input.SecurityReportSha256Digest, input.SandboxImage, input.IsolationPolicy,
            environmentReferences, networkDestinations, decision.BundleId, decision.BundleVersion,
            decision.BundleSha256Digest, true, verification.VerificationEvidenceReference, outcome,
            classification, roles, outputKind, decision.Reasons, decision.EvidenceReferences, decision.DecidedAt);

    private static PackageCoordinate ToCoordinate(SovereignApprovedPackageCoordinate value)
    {
        if (!Enum.TryParse<PackageKind>(value.Kind, false, out var kind) || !Enum.IsDefined(kind))
            throw new UnauthorizedAccessException("OPA returned an invalid Sandbox image kind.");
        var result = new PackageCoordinate(kind, value.Name, value.Version, value.ContentDigest);
        result.Validate();
        if (result.Kind != PackageKind.SandboxImage) throw new UnauthorizedAccessException("OPA did not authorize a Sandbox image.");
        return result;
    }

    private static ImmutableHashSet<string> Normalize(ImmutableArray<string> values)
    {
        if (values.Any(string.IsNullOrWhiteSpace)) throw new UnauthorizedAccessException("OPA returned an invalid Sandbox set.");
        var result = values.ToImmutableHashSet(StringComparer.Ordinal);
        if (result.Count != values.Length) throw new UnauthorizedAccessException("OPA returned a duplicate Sandbox set.");
        return result;
    }

    private static bool IsolationMatches(Sandbox.SandboxIsolationPolicy left, Sandbox.SandboxIsolationPolicy right) =>
        StringComparer.Ordinal.Equals(left.IsolationClass, right.IsolationClass) && left.Ephemeral == right.Ephemeral &&
        left.MicroVmIsolation == right.MicroVmIsolation && left.ProductionCredentialsAllowed == right.ProductionCredentialsAllowed &&
        left.HostFilesystemAccessAllowed == right.HostFilesystemAccessAllowed && left.NetworkDefaultDeny == right.NetworkDefaultDeny &&
        left.CpuLimit == right.CpuLimit && left.MemoryLimitBytes == right.MemoryLimitBytes &&
        left.ExecutionTimeout == right.ExecutionTimeout && left.AllowedNetworkDestinations.SetEquals(right.AllowedNetworkDestinations);

    private static string CoordinateKey(PackageCoordinate value) =>
        $"{value.Kind}|{value.Name}|{value.Version}|{value.ContentDigest}";
}
