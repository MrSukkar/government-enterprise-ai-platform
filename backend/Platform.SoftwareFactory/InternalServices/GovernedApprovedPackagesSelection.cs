using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedApprovedPackagesSelectionRequest(
    Guid SelectionId,
    Guid ArchitectureDiscoveryId,
    Guid SystemsDiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    long ExpectedRegistrationVersion,
    string ExpectedIntentSha256Digest,
    string ExpectedContextSha256Digest,
    string ExpectedInventorySha256Digest,
    string ExpectedArchitectureSha256Digest,
    ImmutableArray<PackageCoordinate> RequestedCoordinates,
    GovernedIdentity Identity,
    string Purpose,
    DataClassification MaximumClassification,
    string AuthorizationEvidenceReference,
    string Environment,
    IntentPolicyBundleReference PolicyBundle,
    DateTimeOffset RequestedAt)
{
    public GovernedApprovedPackagesSelectionRequest Validate()
    {
        if (SelectionId == Guid.Empty || ArchitectureDiscoveryId == Guid.Empty ||
            SystemsDiscoveryId == Guid.Empty || ContextDiscoveryId == Guid.Empty || RegistrationId == Guid.Empty)
            throw new InvalidOperationException("Approved Packages selection and prerequisite identities are required.");
        if (ExpectedRegistrationVersion < 0) throw new InvalidOperationException("A persisted registration version is required.");
        ValidateDigest(ExpectedIntentSha256Digest, "intent");
        ValidateDigest(ExpectedContextSha256Digest, "context");
        ValidateDigest(ExpectedInventorySha256Digest, "inventory");
        ValidateDigest(ExpectedArchitectureSha256Digest, "architecture");
        if (RequestedCoordinates.IsDefaultOrEmpty) throw new InvalidOperationException("At least one exact package coordinate is required.");
        foreach (var coordinate in RequestedCoordinates) ValidateExactCoordinate(coordinate);
        if (RequestedCoordinates.Distinct().Count() != RequestedCoordinates.Length)
            throw new InvalidOperationException("Duplicate package coordinates are not allowed.");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("developer.internal-service.packages.select"))
            throw new UnauthorizedAccessException("Governed Approved Packages selection permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Identity.SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Identity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for package selection.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentNullException.ThrowIfNull(PolicyBundle);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment))
            throw new InvalidOperationException("Package-selection policy environment does not match the request.");
        if (RequestedAt == default || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Package-selection time is invalid for the active policy bundle.");
        return this;
    }

    internal static void ValidateDigest(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException($"Expected {field} requires a SHA-256 digest.");
    }

    internal static void ValidateExactCoordinate(PackageCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(coordinate);
        coordinate.Validate();
        if (!Enum.IsDefined(coordinate.Kind) || coordinate.Version.Equals("latest", StringComparison.OrdinalIgnoreCase) ||
            coordinate.Version.IndexOfAny(['*', '[', ']', '(', ')', '>', '<', ',']) >= 0)
            throw new InvalidOperationException("Package coordinates require an exact immutable version.");
        var separator = coordinate.ContentDigest.IndexOf(':');
        if (separator <= 0 || separator == coordinate.ContentDigest.Length - 1 ||
            coordinate.ContentDigest[(separator + 1)..].Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Package content digest must be algorithm-qualified hexadecimal data.");
    }
}

public interface IAuthorizedExistingArchitectureSnapshotReader
{
    Task<AuthorizedExistingArchitectureDiscoveryReceipt?> LoadAsync(
        Guid architectureDiscoveryId,
        string tenantId,
        CancellationToken cancellationToken);
}

public sealed record ApprovedPackagesPolicyInput(
    Guid DecisionRequestId,
    Guid SelectionId,
    Guid ArchitectureDiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string SubjectId,
    string Purpose,
    string Environment,
    DataClassification MaximumClassification,
    string IntentSha256Digest,
    string ContextSha256Digest,
    string InventorySha256Digest,
    string ArchitectureSha256Digest,
    ImmutableArray<PackageCoordinate> RequestedCoordinates,
    IntentPolicyBundleReference PolicyBundle,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset EvaluatedAt);

public sealed record ApprovedPackagesPolicyDecision(
    Guid DecisionRequestId,
    Guid SelectionId,
    Guid ArchitectureDiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string Environment,
    string IntentSha256Digest,
    string ContextSha256Digest,
    string InventorySha256Digest,
    string ArchitectureSha256Digest,
    string BundleId,
    string BundleVersion,
    string BundleSha256Digest,
    bool PolicySignatureValid,
    string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome,
    DataClassification MaximumClassification,
    ImmutableHashSet<PackageCoordinate> AllowedCoordinates,
    int MaximumResults,
    ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IApprovedPackagesPolicyGate
{
    Task<ApprovedPackagesPolicyDecision> EvaluateAsync(
        ApprovedPackagesPolicyInput input,
        CancellationToken cancellationToken);
}

public sealed record PackageSupplyChainAssuranceRequest(
    Guid SelectionId,
    string TenantId,
    string Environment,
    InstitutionalPackage Package,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RequestedAt);

public sealed record PackageSupplyChainAssuranceDecision(
    Guid SelectionId,
    PackageCoordinate Coordinate,
    string TenantId,
    bool DigestVerified,
    bool ProvenanceVerified,
    bool SbomVerified,
    bool SignatureVerified,
    bool SovereignRegistryVerified,
    bool PackageTransferred,
    bool PackageExecuted,
    bool ExternalEffectOccurred,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IApprovedPackageSupplyChainVerifier
{
    Task<PackageSupplyChainAssuranceDecision> VerifyAsync(
        PackageSupplyChainAssuranceRequest request,
        CancellationToken cancellationToken);
}

public sealed record ApprovedPackageResultAuthorizationRequest(
    Guid AuthorizationRequestId,
    Guid SelectionId,
    string TenantId,
    string SubjectId,
    string Purpose,
    string Action,
    PackageCoordinate Coordinate,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RequestedAt);

public sealed record ApprovedPackageResultAuthorizationDecision(
    Guid AuthorizationRequestId,
    Guid SelectionId,
    string TenantId,
    PackageCoordinate Coordinate,
    bool IsAllowed,
    string Code,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IApprovedPackageResultAuthorizer
{
    Task<ApprovedPackageResultAuthorizationDecision> AuthorizeAsync(
        ApprovedPackageResultAuthorizationRequest request,
        CancellationToken cancellationToken);
}

public sealed record GovernedApprovedPackage(
    PackageCoordinate Coordinate,
    string Source,
    string Publisher,
    string ProvenanceReference,
    string LicenseExpression,
    string SbomReference,
    string SignatureReference,
    string ApprovalEvidenceReference,
    DateTimeOffset ApprovalDecidedAt,
    DateTimeOffset? ApprovalExpiresAt,
    ImmutableArray<string> SupplyChainEvidenceReferences,
    ImmutableArray<string> AuthorizationEvidenceReferences);

public sealed record ApprovedPackagesEvidenceRecord(
    Guid SelectionId,
    Guid ArchitectureDiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string SubjectId,
    string Purpose,
    string ArchitectureSha256Digest,
    Guid PolicyDecisionRequestId,
    string PolicyBundleSha256Digest,
    string SelectionSha256Digest,
    ImmutableArray<GovernedApprovedPackage> Packages,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset SelectedAt);

public sealed record ApprovedPackagesEvidenceReceipt(
    Guid SelectionId,
    Guid ArchitectureDiscoveryId,
    Guid RegistrationId,
    string TenantId,
    string SelectionSha256Digest,
    string EvidenceReference,
    DateTimeOffset RecordedAt);

public interface IApprovedPackagesEvidenceRecorder
{
    Task<ApprovedPackagesEvidenceReceipt> RecordAsync(
        ApprovedPackagesEvidenceRecord record,
        CancellationToken cancellationToken);
}

public sealed class ApprovedPackagesDependencyUnavailableException(string message) : Exception(message);

public sealed record GovernedApprovedPackagesSelectionReceipt(
    Guid SelectionId,
    Guid ArchitectureDiscoveryId,
    Guid SystemsDiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string ArchitectureSha256Digest,
    GovernedIntentPolicyOutcome PolicyOutcome,
    bool IsSelectionReleased,
    bool CanAdvance,
    string? SelectionSha256Digest,
    ImmutableArray<GovernedApprovedPackage> Packages,
    string? SelectionEvidenceReference,
    ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate,
    DateTimeOffset CompletedAt);

public sealed class GovernedApprovedPackagesSelectionEngine(IPackageEligibilityEvaluator eligibilityEvaluator)
{
    public async Task<GovernedApprovedPackagesSelectionReceipt> SelectAsync(
        GovernedApprovedPackagesSelectionRequest request,
        IAuthorizedExistingArchitectureSnapshotReader architectureReader,
        IApprovedPackagesPolicyGate policyGate,
        IInstitutionalPackageRegistryReader registryReader,
        IApprovedPackageSupplyChainVerifier supplyChainVerifier,
        IApprovedPackageResultAuthorizer resultAuthorizer,
        IApprovedPackagesEvidenceRecorder evidenceRecorder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(architectureReader);
        ArgumentNullException.ThrowIfNull(policyGate);
        ArgumentNullException.ThrowIfNull(registryReader);
        ArgumentNullException.ThrowIfNull(supplyChainVerifier);
        ArgumentNullException.ThrowIfNull(resultAuthorizer);
        ArgumentNullException.ThrowIfNull(evidenceRecorder);
        request.Validate();

        var architecture = await architectureReader.LoadAsync(
            request.ArchitectureDiscoveryId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Authorized Existing Architecture snapshot was not found.");
        ValidateArchitecture(request, architecture);

        var prerequisiteEvidence = architecture.EvidenceReferences
            .Append(architecture.DiscoveryEvidenceReference!)
            .Append(request.AuthorizationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var input = new ApprovedPackagesPolicyInput(
            Guid.NewGuid(), request.SelectionId, architecture.DiscoveryId, architecture.RegistrationId,
            architecture.RegistrationVersion, architecture.TenantId, request.Identity.SubjectId,
            request.Purpose, request.Environment, request.MaximumClassification,
            request.ExpectedIntentSha256Digest, request.ExpectedContextSha256Digest,
            request.ExpectedInventorySha256Digest, architecture.ArchitectureSha256Digest!,
            request.RequestedCoordinates, request.PolicyBundle, prerequisiteEvidence, request.RequestedAt);
        var decision = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidateDecision(input, request.Identity, decision);
        var policyEvidence = prerequisiteEvidence.Append(decision.PolicyVerificationEvidenceReference)
            .Concat(decision.EvidenceReferences).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        if (decision.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedApprovedPackagesSelectionReceipt(
                request.SelectionId, architecture.DiscoveryId, architecture.SystemsDiscoveryId,
                architecture.ContextDiscoveryId, architecture.RegistrationId, architecture.RegistrationVersion,
                architecture.TenantId, architecture.ArchitectureSha256Digest!, decision.Outcome,
                false, false, null, [], null, policyEvidence,
                "Policy denial requires a new governed Approved Packages request", decision.DecidedAt);

        var selected = ImmutableArray.CreateBuilder<GovernedApprovedPackage>(decision.AllowedCoordinates.Count);
        foreach (var coordinate in decision.AllowedCoordinates.OrderBy(CoordinateKey, StringComparer.Ordinal))
        {
            var package = await registryReader.FindExactAsync(coordinate, cancellationToken)
                ?? throw new ApprovedPackagesDependencyUnavailableException("An exact authorized package record is unavailable.");
            package.Validate();
            ValidatePackageRecord(package, coordinate, request, decision);
            var eligibility = eligibilityEvaluator.Evaluate(package, new PackageUseRequest(
                coordinate, request.Identity.TenantId, request.Environment, decision.DecidedAt));
            if (!eligibility.IsAllowed)
                throw new UnauthorizedAccessException($"Institutional package eligibility denied: {eligibility.Code}.");

            var approval = package.CurrentApproval!;
            var assuranceRequest = new PackageSupplyChainAssuranceRequest(
                request.SelectionId, architecture.TenantId, request.Environment, package,
                policyEvidence.Append(approval.EvidenceReference!).Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal).ToImmutableArray(), decision.DecidedAt);
            var assurance = await supplyChainVerifier.VerifyAsync(assuranceRequest, cancellationToken);
            ValidateAssurance(request, coordinate, assurance);
            var authorizationRequest = new ApprovedPackageResultAuthorizationRequest(
                Guid.NewGuid(), request.SelectionId, architecture.TenantId, request.Identity.SubjectId,
                request.Purpose, "approved-package.read", coordinate,
                assurance.EvidenceReferences.Concat(policyEvidence).Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal).ToImmutableArray(), assurance.DecidedAt);
            var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
            ValidateAuthorization(authorizationRequest, authorization);

            selected.Add(new GovernedApprovedPackage(
                coordinate, package.Provenance.Source, package.Provenance.Publisher,
                package.Provenance.ProvenanceReference, package.LicenseExpression,
                package.SbomReference!, package.SignatureReference!, approval.EvidenceReference!,
                approval.DecidedAt, approval.ExpiresAt,
                assurance.EvidenceReferences.Order(StringComparer.Ordinal).ToImmutableArray(),
                authorization.EvidenceReferences.Order(StringComparer.Ordinal).ToImmutableArray()));
        }

        var packages = selected.OrderBy(item => CoordinateKey(item.Coordinate), StringComparer.Ordinal).ToImmutableArray();
        if (packages.Length != request.RequestedCoordinates.Length)
            throw new InvalidOperationException("Approved Packages selection cannot partially substitute or omit coordinates.");
        var digest = Digest(architecture, decision, packages);
        var evidence = policyEvidence.Concat(packages.SelectMany(item => item.SupplyChainEvidenceReferences))
            .Concat(packages.SelectMany(item => item.AuthorizationEvidenceReferences))
            .Concat(packages.Select(item => item.ApprovalEvidenceReference))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new ApprovedPackagesEvidenceRecord(
            request.SelectionId, architecture.DiscoveryId, architecture.RegistrationId,
            architecture.RegistrationVersion, architecture.TenantId, request.Identity.SubjectId,
            request.Purpose, architecture.ArchitectureSha256Digest!, decision.DecisionRequestId,
            decision.BundleSha256Digest, digest, packages, evidence, decision.DecidedAt);
        var receipt = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidenceReceipt(record, receipt);

        return new GovernedApprovedPackagesSelectionReceipt(
            request.SelectionId, architecture.DiscoveryId, architecture.SystemsDiscoveryId,
            architecture.ContextDiscoveryId, architecture.RegistrationId, architecture.RegistrationVersion,
            architecture.TenantId, architecture.ArchitectureSha256Digest!, decision.Outcome,
            true, false, digest, packages, receipt.EvidenceReference,
            evidence.Append(receipt.EvidenceReference).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved AI Planning", receipt.RecordedAt);
    }

    private static void ValidateArchitecture(
        GovernedApprovedPackagesSelectionRequest request,
        AuthorizedExistingArchitectureDiscoveryReceipt architecture)
    {
        if (architecture.DiscoveryId != request.ArchitectureDiscoveryId ||
            architecture.SystemsDiscoveryId != request.SystemsDiscoveryId ||
            architecture.ContextDiscoveryId != request.ContextDiscoveryId ||
            architecture.RegistrationId != request.RegistrationId ||
            architecture.RegistrationVersion != request.ExpectedRegistrationVersion ||
            !StringComparer.Ordinal.Equals(architecture.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(architecture.IntentSha256Digest, request.ExpectedIntentSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(architecture.ContextSha256Digest, request.ExpectedContextSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(architecture.InventorySha256Digest, request.ExpectedInventorySha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(architecture.ArchitectureSha256Digest, request.ExpectedArchitectureSha256Digest))
            throw new InvalidOperationException("Existing Architecture snapshot does not match the package-selection request.");
        if (architecture.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !architecture.IsArchitectureReleased ||
            architecture.CanAdvance || architecture.Items.IsDefaultOrEmpty ||
            string.IsNullOrWhiteSpace(architecture.ArchitectureSha256Digest) ||
            string.IsNullOrWhiteSpace(architecture.DiscoveryEvidenceReference))
            throw new UnauthorizedAccessException("Existing Architecture snapshot is not eligible for package selection.");
        GovernedApprovedPackagesSelectionRequest.ValidateDigest(architecture.ArchitectureSha256Digest, "architecture");
        ValidateEvidence(architecture.EvidenceReferences, "Existing Architecture snapshot");
    }

    private static void ValidateDecision(
        ApprovedPackagesPolicyInput input,
        GovernedIdentity identity,
        ApprovedPackagesPolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.DecisionRequestId != input.DecisionRequestId || decision.SelectionId != input.SelectionId ||
            decision.ArchitectureDiscoveryId != input.ArchitectureDiscoveryId ||
            decision.RegistrationId != input.RegistrationId || decision.RegistrationVersion != input.RegistrationVersion ||
            !StringComparer.Ordinal.Equals(decision.TenantId, input.TenantId) ||
            !StringComparer.Ordinal.Equals(decision.Environment, input.Environment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.IntentSha256Digest, input.IntentSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.ContextSha256Digest, input.ContextSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.InventorySha256Digest, input.InventorySha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.ArchitectureSha256Digest, input.ArchitectureSha256Digest) ||
            !StringComparer.Ordinal.Equals(decision.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(decision.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Approved Packages decision; selection denied fail closed.");
        if (!decision.PolicySignatureValid || string.IsNullOrWhiteSpace(decision.PolicyVerificationEvidenceReference) ||
            decision.MaximumClassification > input.MaximumClassification || decision.MaximumClassification > identity.Clearance)
            throw new UnauthorizedAccessException("Approved Packages policy signature or scope is invalid.");
        if (decision.Reasons.IsDefaultOrEmpty || decision.EvidenceReferences.IsDefaultOrEmpty || decision.DecidedAt < input.EvaluatedAt)
            throw new InvalidOperationException("Approved Packages policy decision requires current reasons and evidence.");
        ValidateEvidence(decision.EvidenceReferences, "Approved Packages OPA decision");
        if (decision.Outcome == GovernedIntentPolicyOutcome.Permit &&
            (decision.AllowedCoordinates.IsEmpty || decision.MaximumResults != input.RequestedCoordinates.Length ||
             decision.MaximumResults <= 0 || !decision.AllowedCoordinates.SetEquals(input.RequestedCoordinates)))
            throw new UnauthorizedAccessException("OPA permit did not authorize the exact requested package set.");
        foreach (var coordinate in decision.AllowedCoordinates)
            GovernedApprovedPackagesSelectionRequest.ValidateExactCoordinate(coordinate);
    }

    private static void ValidatePackageRecord(
        InstitutionalPackage package,
        PackageCoordinate coordinate,
        GovernedApprovedPackagesSelectionRequest request,
        ApprovedPackagesPolicyDecision decision)
    {
        if (package.Coordinate != coordinate || !decision.AllowedCoordinates.Contains(package.Coordinate))
            throw new UnauthorizedAccessException("Institutional registry returned a substituted package coordinate.");
        if (!package.AllowedTenantIds.Contains(request.Identity.TenantId) ||
            !package.AllowedEnvironments.Contains(request.Environment) || !package.AvailableInSovereignRegistry)
            throw new UnauthorizedAccessException("Institutional package scope or sovereign availability is invalid.");
        var approval = package.CurrentApproval;
        if (approval is null || approval.Status != PackageApprovalStatus.Approved ||
            approval.ExpiresAt <= decision.DecidedAt || string.IsNullOrWhiteSpace(approval.EvidenceReference))
            throw new UnauthorizedAccessException("A current evidence-bearing institutional package approval is required.");
        if (string.IsNullOrWhiteSpace(package.SbomReference) || string.IsNullOrWhiteSpace(package.SignatureReference))
            throw new UnauthorizedAccessException("Approved packages require SBOM and signature references.");
    }

    private static void ValidateAssurance(
        GovernedApprovedPackagesSelectionRequest request,
        PackageCoordinate coordinate,
        PackageSupplyChainAssuranceDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.SelectionId != request.SelectionId || decision.Coordinate != coordinate ||
            !StringComparer.Ordinal.Equals(decision.TenantId, request.Identity.TenantId) ||
            !decision.DigestVerified || !decision.ProvenanceVerified || !decision.SbomVerified ||
            !decision.SignatureVerified || !decision.SovereignRegistryVerified ||
            decision.PackageTransferred || decision.PackageExecuted || decision.ExternalEffectOccurred)
            throw new UnauthorizedAccessException("Package supply-chain assurance denied or performed a forbidden effect.");
        ValidateEvidence(decision.EvidenceReferences, "Package supply-chain assurance");
    }

    private static void ValidateAuthorization(
        ApprovedPackageResultAuthorizationRequest request,
        ApprovedPackageResultAuthorizationDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.AuthorizationRequestId != request.AuthorizationRequestId ||
            decision.SelectionId != request.SelectionId || decision.Coordinate != request.Coordinate ||
            !StringComparer.Ordinal.Equals(decision.TenantId, request.TenantId) || !decision.IsAllowed ||
            string.IsNullOrWhiteSpace(decision.Code) || decision.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Approved Package result authorization denied or mismatched.");
        ValidateEvidence(decision.EvidenceReferences, "Approved Package result authorization");
    }

    private static string Digest(
        AuthorizedExistingArchitectureDiscoveryReceipt architecture,
        ApprovedPackagesPolicyDecision decision,
        ImmutableArray<GovernedApprovedPackage> packages)
    {
        var canonical = new StringBuilder().Append(architecture.DiscoveryId.ToString("D")).Append('|')
            .Append(architecture.ArchitectureSha256Digest!.ToLowerInvariant()).Append('|')
            .Append(decision.DecisionRequestId.ToString("D")).Append('|')
            .Append(decision.BundleSha256Digest.ToLowerInvariant());
        foreach (var item in packages)
            canonical.Append('|').Append(CoordinateKey(item.Coordinate)).Append(':')
                .Append(item.ProvenanceReference).Append(':').Append(item.SbomReference).Append(':')
                .Append(item.SignatureReference).Append(':').Append(item.ApprovalEvidenceReference).Append(':')
                .Append(item.ApprovalDecidedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)).Append(':')
                .AppendJoin(',', item.SupplyChainEvidenceReferences).Append(':')
                .AppendJoin(',', item.AuthorizationEvidenceReferences);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }

    private static string CoordinateKey(PackageCoordinate coordinate) =>
        $"{coordinate.Kind}|{coordinate.Name}|{coordinate.Version}|{coordinate.ContentDigest}";

    private static void ValidateEvidenceReceipt(ApprovedPackagesEvidenceRecord record, ApprovedPackagesEvidenceReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (receipt.SelectionId != record.SelectionId || receipt.ArchitectureDiscoveryId != record.ArchitectureDiscoveryId ||
            receipt.RegistrationId != record.RegistrationId || !StringComparer.Ordinal.Equals(receipt.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.SelectionSha256Digest, record.SelectionSha256Digest) ||
            string.IsNullOrWhiteSpace(receipt.EvidenceReference) || receipt.RecordedAt < record.SelectedAt)
            throw new InvalidOperationException("Approved Packages evidence recorder returned a mismatched receipt.");
    }

    private static void ValidateEvidence(ImmutableArray<string> references, string owner)
    {
        if (references.IsDefaultOrEmpty || references.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException($"{owner} requires non-placeholder evidence references.");
    }
}
