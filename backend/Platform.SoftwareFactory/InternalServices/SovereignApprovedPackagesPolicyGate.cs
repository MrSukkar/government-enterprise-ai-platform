using System.Collections.Immutable;
using System.Globalization;
using Platform.Domain.Security;
using Platform.Governance.Policies;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.InternalService;

public sealed class SovereignApprovedPackagesPolicyGate(
    IPolicyBundleVerifier policyBundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : IApprovedPackagesPolicyGate
{
    public async Task<ApprovedPackagesPolicyDecision> EvaluateAsync(
        ApprovedPackagesPolicyInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!StringComparer.Ordinal.Equals(input.Environment, input.PolicyBundle.Environment))
            throw new UnauthorizedAccessException("Approved Packages policy environment is invalid.");
        var bundle = new SignedPolicyBundleReference(input.PolicyBundle.BundleId,
            input.PolicyBundle.Version, input.PolicyBundle.Sha256Digest,
            input.PolicyBundle.SignatureReference, input.PolicyBundle.Environment,
            input.PolicyBundle.ActivatedAt);
        var verification = await policyBundleVerifier.VerifyAsync(bundle, cancellationToken);
        var evaluatedAt = verification.VerifiedAt > input.EvaluatedAt ? verification.VerifiedAt : input.EvaluatedAt;
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["selectionId"] = input.SelectionId.ToString("D"),
            ["architectureDiscoveryId"] = input.ArchitectureDiscoveryId.ToString("D"),
            ["registrationId"] = input.RegistrationId.ToString("D"),
            ["registrationVersion"] = input.RegistrationVersion.ToString(CultureInfo.InvariantCulture),
            ["intentSha256Digest"] = input.IntentSha256Digest,
            ["contextSha256Digest"] = input.ContextSha256Digest,
            ["inventorySha256Digest"] = input.InventorySha256Digest,
            ["architectureSha256Digest"] = input.ArchitectureSha256Digest,
            ["coordinateCount"] = input.RequestedCoordinates.Length.ToString(CultureInfo.InvariantCulture)
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences.Append(verification.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(new SovereignPolicyEvaluationRequest(
            input.DecisionRequestId, "internal-service.approved-packages.select",
            input.ArchitectureDiscoveryId.ToString("D"), input.TenantId, input.SubjectId,
            input.Purpose, input.MaximumClassification.ToString(), input.Environment,
            verification, attributes, evidence, evaluatedAt), cancellationToken);

        if (decision.Outcome == OpaDecisionOutcome.Deny)
        {
            if (decision.Scope is not null)
                throw new UnauthorizedAccessException("Denied Approved Packages decision returned scope.");
            return Create(input, verification, decision, GovernedIntentPolicyOutcome.Deny,
                input.MaximumClassification, [], [], 0);
        }

        var envelope = decision.Scope
            ?? throw new UnauthorizedAccessException("OPA permit omitted Approved Packages scope.");
        var scope = envelope.ApprovedPackages
            ?? throw new UnauthorizedAccessException("OPA permit omitted action-specific Approved Packages scope.");
        if (envelope.ExistingSystems is not null || envelope.ExistingArchitecture is not null ||
            envelope.AiPlanning is not null || envelope.CodeGeneration is not null || envelope.StaticValidation is not null ||
            envelope.SecurityValidation is not null || envelope.Sandbox is not null || envelope.Tests is not null || envelope.HumanReview is not null || envelope.Git is not null ||
            !envelope.AllowedResourceIds.IsDefaultOrEmpty || !envelope.AllowedModalities.IsDefaultOrEmpty ||
            !envelope.RequiredRoles.IsDefaultOrEmpty || envelope.MaximumResults != 0 ||
            !string.IsNullOrWhiteSpace(envelope.MaximumClassification))
            throw new UnauthorizedAccessException("Approved Packages decision mixed scopes from another action.");
        if (!Enum.TryParse<DataClassification>(scope.MaximumClassification, false, out var maximumClassification) ||
            !Enum.IsDefined(maximumClassification) || scope.AllowedCoordinates.IsDefaultOrEmpty ||
            scope.RequiredRoles.IsDefaultOrEmpty || scope.MaximumResults <= 0)
            throw new UnauthorizedAccessException("OPA returned invalid Approved Packages scope.");

        var coordinates = scope.AllowedCoordinates.Select(MapCoordinate).ToImmutableHashSet();
        if (coordinates.Count != scope.AllowedCoordinates.Length)
            throw new UnauthorizedAccessException("OPA returned duplicate Approved Packages coordinates.");
        var roles = Normalize(scope.RequiredRoles);
        return Create(input, verification, decision, GovernedIntentPolicyOutcome.Permit,
            maximumClassification, coordinates, roles, scope.MaximumResults);
    }

    private static PackageCoordinate MapCoordinate(SovereignApprovedPackageCoordinate value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Enum.TryParse<PackageKind>(value.Kind, false, out var kind) || !Enum.IsDefined(kind))
            throw new UnauthorizedAccessException("OPA returned invalid package kind.");
        var coordinate = new PackageCoordinate(kind, value.Name, value.Version, value.ContentDigest);
        GovernedApprovedPackagesSelectionRequest.ValidateExactCoordinate(coordinate);
        if (!coordinate.ContentDigest.StartsWith("sha256:", StringComparison.Ordinal) ||
            coordinate.ContentDigest.Length != 71)
            throw new UnauthorizedAccessException("Wave 06 requires SHA-256 package coordinates.");
        return coordinate;
    }

    private static ImmutableHashSet<string> Normalize(ImmutableArray<string> values)
    {
        foreach (var value in values) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var result = values.ToImmutableHashSet(StringComparer.Ordinal);
        if (result.Count != values.Length) throw new UnauthorizedAccessException("OPA returned duplicate required roles.");
        return result;
    }

    private static ApprovedPackagesPolicyDecision Create(
        ApprovedPackagesPolicyInput input, PolicyBundleVerification verification,
        SovereignPolicyEvaluationDecision decision, GovernedIntentPolicyOutcome outcome,
        DataClassification maximumClassification, ImmutableHashSet<PackageCoordinate> coordinates,
        ImmutableHashSet<string> roles, int maximumResults) => new(
            decision.DecisionRequestId, input.SelectionId, input.ArchitectureDiscoveryId,
            input.RegistrationId, input.RegistrationVersion, input.TenantId, input.Environment,
            input.IntentSha256Digest, input.ContextSha256Digest, input.InventorySha256Digest,
            input.ArchitectureSha256Digest, decision.BundleId, decision.BundleVersion,
            decision.BundleSha256Digest, PolicySignatureValid: true,
            verification.VerificationEvidenceReference, outcome, maximumClassification,
            coordinates, roles, maximumResults, decision.Reasons, decision.EvidenceReferences,
            decision.DecidedAt);
}
