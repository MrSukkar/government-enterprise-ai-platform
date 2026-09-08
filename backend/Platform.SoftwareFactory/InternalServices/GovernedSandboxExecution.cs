using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.Packages;
using Platform.SoftwareFactory.Sandbox;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedSandboxExecutionRequest(
    Guid ExecutionId, Guid SecurityValidationId, Guid GenerationId, Guid DeliveryRunId,
    string ExpectedCandidateSha256Digest, string ExpectedSecurityReportSha256Digest,
    string ExpectedSecurityEvidenceReference, PackageCoordinate SandboxImage,
    SandboxIsolationPolicy IsolationPolicy,
    ImmutableDictionary<string, string> NonSecretEnvironmentReferences,
    GovernedIdentity Identity, string Purpose, DataClassification MaximumClassification,
    string AuthorizationEvidenceReference, string Environment,
    IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedSandboxExecutionRequest Validate()
    {
        if (ExecutionId == Guid.Empty || SecurityValidationId == Guid.Empty || GenerationId == Guid.Empty || DeliveryRunId == Guid.Empty)
            throw new InvalidOperationException("Sandbox and prerequisite identities are required.");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedCandidateSha256Digest, "code candidate");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedSecurityReportSha256Digest, "Security Validation report");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedSecurityEvidenceReference);
        SandboxImage.Validate();
        if (SandboxImage.Kind != PackageKind.SandboxImage)
            throw new InvalidOperationException("An exact institutional sandbox image is required.");
        IsolationPolicy.Validate();
        if (NonSecretEnvironmentReferences is null || NonSecretEnvironmentReferences.Any(item =>
                string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value) ||
                IsSecretLike(item.Key) || IsSecretLike(item.Value)))
            throw new InvalidOperationException("Only non-secret environment references are permitted.");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("developer.internal-service.sandbox.execute"))
            throw new UnauthorizedAccessException("Governed Sandbox execution permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Sandbox execution.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Sandbox policy environment or time is invalid.");
        return this;
    }

    private static bool IsSecretLike(string value) =>
        value.Contains("SECRET", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("TOKEN", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("CONNECTIONSTRING", StringComparison.OrdinalIgnoreCase);
}

public interface IAuthorizedSecurityValidationReceiptReader
{
    Task<GovernedSecurityValidationReceipt?> LoadAsync(Guid validationId, string tenantId, CancellationToken cancellationToken);
}

public interface ISandboxSecurityValidationReceiptReader
{
    Task<GovernedSecurityValidationReceipt?> LoadAsync(
        Guid validationId, string tenantId, string purpose, Guid generationId, Guid deliveryRunId,
        string candidateSha256Digest, string securityReportSha256Digest,
        string securityEvidenceReference, CancellationToken cancellationToken);
}

public interface ISandboxCodeGenerationCandidateReader
{
    Task<AuthorizedCodeGenerationCandidateSnapshot?> LoadAsync(
        Guid generationId, string tenantId, string purpose, string candidateSha256Digest,
        Guid securityValidationId, string securityEvidenceReference, CancellationToken cancellationToken);
}

public interface ISandboxDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, string purpose,
        Guid generationId, Guid securityValidationId, string candidateSha256Digest,
        string securityReportSha256Digest, CancellationToken cancellationToken);
}

public sealed record SandboxPolicyInput(
    Guid DecisionRequestId, Guid ExecutionId, Guid SecurityValidationId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose, string Environment, DataClassification MaximumClassification,
    string CandidateSha256Digest, string SecurityReportSha256Digest, string SecurityEvidenceReference,
    PackageCoordinate SandboxImage, SandboxIsolationPolicy IsolationPolicy,
    ImmutableDictionary<string, string> NonSecretEnvironmentReferences,
    IntentPolicyBundleReference PolicyBundle, ImmutableArray<string> EvidenceReferences, DateTimeOffset EvaluatedAt);

public sealed record SandboxPolicyDecision(
    Guid DecisionRequestId, Guid ExecutionId, Guid SecurityValidationId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, string Environment, string CandidateSha256Digest, string SecurityReportSha256Digest,
    PackageCoordinate SandboxImage, SandboxIsolationPolicy IsolationPolicy,
    ImmutableDictionary<string, string> AllowedEnvironmentReferences,
    ImmutableHashSet<string> AllowedNetworkDestinations,
    string BundleId, string BundleVersion, string BundleSha256Digest, bool PolicySignatureValid,
    string PolicyVerificationEvidenceReference, GovernedIntentPolicyOutcome Outcome,
    DataClassification MaximumClassification, ImmutableHashSet<string> RequiredRoles, string OutputKind,
    ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ISandboxPolicyGate
{
    Task<SandboxPolicyDecision> EvaluateAsync(SandboxPolicyInput input, CancellationToken cancellationToken);
}

public sealed record SandboxResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid ExecutionId, string TenantId, string SubjectId, string Purpose,
    string Environment, DataClassification MaximumClassification, string CandidateSha256Digest,
    string SecurityReportSha256Digest, PackageCoordinate SandboxImage, SandboxIsolationPolicy IsolationPolicy,
    ImmutableDictionary<string, string> EnvironmentReferences, SandboxExecutionResult Result,
    string ResultSha256Digest, GovernedIdentity Identity, ImmutableHashSet<string> RequiredRoles,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record SandboxResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid ExecutionId, string TenantId, string ResultSha256Digest,
    bool IsAllowed, string Code, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ISandboxResultAuthorizer
{
    Task<SandboxResultAuthorizationDecision> AuthorizeAsync(SandboxResultAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record SandboxEvidenceRecord(
    Guid ExecutionId, Guid SecurityValidationId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, Guid PolicyDecisionRequestId,
    string CandidateSha256Digest, string SecurityReportSha256Digest,
    string SecurityEvidenceReference, PackageCoordinate SandboxImage,
    SandboxIsolationPolicy IsolationPolicy, SandboxExecutionResult Result, string ResultSha256Digest,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset ExecutedAt);

public sealed record SandboxEvidenceReceipt(
    Guid ExecutionId, Guid DeliveryRunId, string TenantId, string ResultSha256Digest,
    string EvidenceReference, DateTimeOffset RecordedAt);

public interface ISandboxEvidenceRecorder
{
    Task<SandboxEvidenceReceipt> RecordAsync(SandboxEvidenceRecord record, CancellationToken cancellationToken);
}

public sealed class SandboxDependencyUnavailableException(string message) : Exception(message);

public sealed record GovernedSandboxExecutionReceipt(
    Guid ExecutionId, Guid SecurityValidationId, Guid GenerationId, Guid DeliveryRunId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, bool IsAccepted, bool ProductionEffectOccurred, bool CanAdvance,
    string CandidateSha256Digest, string SecurityReportSha256Digest, PackageCoordinate SandboxImage,
    int? ExitCode, bool? TimedOut, bool? IsolationViolationDetected,
    ImmutableArray<string> ProducedArtifactReferences, string? ResultSha256Digest,
    string? ExecutionEvidenceReference, ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedSandboxExecutionEngine(IPackageEligibilityEvaluator eligibilityEvaluator)
{
    public async Task<GovernedSandboxExecutionReceipt> ExecuteAsync(
        GovernedSandboxExecutionRequest request, ISandboxPolicyGate policyGate,
        ISandboxSecurityValidationReceiptReader securityReader,
        ISandboxCodeGenerationCandidateReader candidateReader, ISandboxDeliveryRunReader runReader,
        IInstitutionalPackageRegistryReader registryReader, IApprovedPackageSupplyChainVerifier supplyChainVerifier,
        ISecuritySandboxRuntime runtime, ISandboxResultAuthorizer resultAuthorizer,
        ISandboxEvidenceRecorder evidenceRecorder, CancellationToken cancellationToken)
    {
        request.Validate();
        var initialEvidence = ImmutableArray.Create(request.ExpectedSecurityEvidenceReference, request.AuthorizationEvidenceReference);
        var input = new SandboxPolicyInput(
            Guid.NewGuid(), request.ExecutionId, request.SecurityValidationId, request.GenerationId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, request.ExpectedCandidateSha256Digest,
            request.ExpectedSecurityReportSha256Digest, request.ExpectedSecurityEvidenceReference,
            request.SandboxImage, request.IsolationPolicy, request.NonSecretEnvironmentReferences,
            request.PolicyBundle, initialEvidence, request.RequestedAt);
        var decision = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidateDecision(input, request.Identity, decision);
        var policyEvidence = initialEvidence.Append(decision.PolicyVerificationEvidenceReference)
            .Concat(decision.EvidenceReferences).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        if (decision.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedSandboxExecutionReceipt(
                request.ExecutionId, request.SecurityValidationId, request.GenerationId, request.DeliveryRunId,
                request.Identity.TenantId, decision.Outcome, false, false, false,
                request.ExpectedCandidateSha256Digest, request.ExpectedSecurityReportSha256Digest,
                request.SandboxImage, null, null, null, [], null, null, policyEvidence,
                "Policy denial requires a new governed Sandbox request", decision.DecidedAt);

        var security = await securityReader.LoadAsync(request.SecurityValidationId, request.Identity.TenantId,
            request.Purpose, request.GenerationId, request.DeliveryRunId, request.ExpectedCandidateSha256Digest,
            request.ExpectedSecurityReportSha256Digest, request.ExpectedSecurityEvidenceReference, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Security Validation receipt was not found.");
        ValidateSecurity(request, security);
        var candidate = await candidateReader.LoadAsync(request.GenerationId, request.Identity.TenantId,
            request.Purpose, request.ExpectedCandidateSha256Digest, request.SecurityValidationId,
            request.ExpectedSecurityEvidenceReference, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Code Generation candidate was not found.");
        ValidateCandidate(request, candidate);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, request.Purpose,
            request.GenerationId, request.SecurityValidationId, request.ExpectedCandidateSha256Digest,
            request.ExpectedSecurityReportSha256Digest, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);

        var package = await registryReader.FindExactAsync(
            request.SandboxImage, request.Identity.TenantId, request.Environment, cancellationToken)
            ?? throw new SandboxDependencyUnavailableException("The exact authorized sandbox image is unavailable.");
        ValidatePackage(request, package, decision.DecidedAt);
        var eligibility = eligibilityEvaluator.Evaluate(package,
            new PackageUseRequest(request.SandboxImage, request.Identity.TenantId, request.Environment, decision.DecidedAt));
        if (!eligibility.IsAllowed)
            throw new UnauthorizedAccessException($"Sandbox image eligibility denied: {eligibility.Code}.");
        var assurance = await supplyChainVerifier.VerifyAsync(new PackageSupplyChainAssuranceRequest(
            request.ExecutionId, request.Identity.TenantId, request.Environment, package, policyEvidence,
            decision.DecidedAt), cancellationToken);
        ValidateAssurance(request, assurance);

        var result = await new GovernedSandboxService(runtime).ExecuteAsync(new SandboxExecutionRequest(
            run, candidate.Candidate, request.SandboxImage, eligibility, request.IsolationPolicy,
            request.NonSecretEnvironmentReferences), cancellationToken);
        ValidateResult(result);
        var resultDigest = Digest(request, decision, result);
        var resultEvidence = policyEvidence.Concat(security.EvidenceReferences)
            .Append(security.ValidationEvidenceReference!).Concat(candidate.Receipt.EvidenceReferences)
            .Append(candidate.Receipt.GenerationEvidenceReference!).Append(run.History[^1].EvidenceReference)
            .Concat(assurance.EvidenceReferences).Append(result.EvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var authorizationRequest = new SandboxResultAuthorizationRequest(
            Guid.NewGuid(), request.ExecutionId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Purpose, request.Environment, request.MaximumClassification,
            request.ExpectedCandidateSha256Digest, request.ExpectedSecurityReportSha256Digest,
            request.SandboxImage, request.IsolationPolicy, request.NonSecretEnvironmentReferences,
            result, resultDigest, request.Identity, decision.RequiredRoles, resultEvidence, assurance.DecidedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
        ValidateAuthorization(authorizationRequest, authorization);
        var allEvidence = resultEvidence.Concat(authorization.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new SandboxEvidenceRecord(
            request.ExecutionId, request.SecurityValidationId, request.GenerationId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, decision.DecisionRequestId, request.ExpectedCandidateSha256Digest,
            request.ExpectedSecurityReportSha256Digest, request.ExpectedSecurityEvidenceReference, request.SandboxImage,
            request.IsolationPolicy, result, resultDigest, allEvidence, authorization.DecidedAt);
        var evidence = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidence(record, evidence);
        return new GovernedSandboxExecutionReceipt(
            request.ExecutionId, request.SecurityValidationId, request.GenerationId, request.DeliveryRunId,
            request.Identity.TenantId, decision.Outcome, true, false, false,
            request.ExpectedCandidateSha256Digest, request.ExpectedSecurityReportSha256Digest,
            request.SandboxImage, result.ExitCode, result.TimedOut, result.IsolationViolationDetected,
            result.ProducedArtifactReferences, resultDigest, evidence.EvidenceReference,
            allEvidence.Append(evidence.EvidenceReference).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved Tests", evidence.RecordedAt);
    }

    private static void ValidateDecision(SandboxPolicyInput input, GovernedIdentity identity, SandboxPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.ExecutionId != input.ExecutionId ||
            value.SecurityValidationId != input.SecurityValidationId || value.GenerationId != input.GenerationId ||
            value.DeliveryRunId != input.DeliveryRunId || !StringComparer.Ordinal.Equals(value.TenantId, input.TenantId) ||
            !StringComparer.Ordinal.Equals(value.Environment, input.Environment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, input.CandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SecurityReportSha256Digest, input.SecurityReportSha256Digest) ||
            value.SandboxImage != input.SandboxImage || !IsolationMatches(value.IsolationPolicy, input.IsolationPolicy) ||
            !StringComparer.Ordinal.Equals(value.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(value.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Sandbox decision.");
        if (!value.PolicySignatureValid || string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) ||
            value.MaximumClassification > input.MaximumClassification || value.MaximumClassification > identity.Clearance ||
            value.Reasons.IsDefaultOrEmpty || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("Sandbox OPA decision is invalid.");
        if (value.Outcome == GovernedIntentPolicyOutcome.Permit &&
            (value.RequiredRoles.IsEmpty || !StringComparer.Ordinal.Equals(value.OutputKind, "sandbox-result") ||
             value.AllowedEnvironmentReferences.Count != input.NonSecretEnvironmentReferences.Count ||
             value.AllowedEnvironmentReferences.Any(item => !input.NonSecretEnvironmentReferences.TryGetValue(item.Key, out var expected) ||
                 !StringComparer.Ordinal.Equals(item.Value, expected)) ||
             !value.AllowedNetworkDestinations.SetEquals(input.IsolationPolicy.AllowedNetworkDestinations)))
            throw new UnauthorizedAccessException("Sandbox OPA permit scope is incomplete.");
        if (value.Outcome != GovernedIntentPolicyOutcome.Permit &&
            (value.RequiredRoles.Count != 0 || value.AllowedEnvironmentReferences.Count != 0 ||
             value.AllowedNetworkDestinations.Count != 0 || !string.IsNullOrEmpty(value.OutputKind)))
            throw new UnauthorizedAccessException("Sandbox OPA denial returned scope.");
    }

    private static bool IsolationMatches(SandboxIsolationPolicy left, SandboxIsolationPolicy right) =>
        StringComparer.Ordinal.Equals(left.IsolationClass, right.IsolationClass) &&
        left.Ephemeral == right.Ephemeral && left.MicroVmIsolation == right.MicroVmIsolation &&
        left.ProductionCredentialsAllowed == right.ProductionCredentialsAllowed &&
        left.HostFilesystemAccessAllowed == right.HostFilesystemAccessAllowed &&
        left.NetworkDefaultDeny == right.NetworkDefaultDeny && left.CpuLimit == right.CpuLimit &&
        left.MemoryLimitBytes == right.MemoryLimitBytes && left.ExecutionTimeout == right.ExecutionTimeout &&
        left.AllowedNetworkDestinations.SetEquals(right.AllowedNetworkDestinations);

    private static void ValidateSecurity(GovernedSandboxExecutionRequest request, GovernedSecurityValidationReceipt value)
    {
        if (value.ValidationId != request.SecurityValidationId || value.GenerationId != request.GenerationId ||
            value.DeliveryRunId != request.DeliveryRunId || !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SecurityReportSha256Digest, request.ExpectedSecurityReportSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.ValidationEvidenceReference, request.ExpectedSecurityEvidenceReference) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || value.Gate != Validation.ValidationGate.Security ||
            !value.IsAccepted || value.IsExecutable || value.CanAdvance || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Security Validation prerequisite is invalid or mismatched.");
    }

    private static void ValidateCandidate(GovernedSandboxExecutionRequest request, AuthorizedCodeGenerationCandidateSnapshot value)
    {
        value.Candidate.Validate();
        if (value.Receipt.GenerationId != request.GenerationId || value.Receipt.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.Receipt.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.Receipt.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            value.Receipt.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.Receipt.IsCodeCandidateReleased ||
            value.Receipt.IsExecutable || value.Receipt.IsApplied || value.Receipt.CanAdvance ||
            !StringComparer.Ordinal.Equals(value.Receipt.Content, value.Candidate.Content) ||
            value.Receipt.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Code candidate is invalid or mismatched.");
    }

    private static void ValidateRun(GovernedSandboxExecutionRequest request, SoftwareDeliveryRun run)
    {
        run.Validate();
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.SecurityValidation).ToArray();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.SecurityValidation || run.History.Length != expected.Length ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) || index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run is not stopped at a valid SecurityValidation boundary.");
    }

    private static void ValidatePackage(GovernedSandboxExecutionRequest request, InstitutionalPackage package, DateTimeOffset decidedAt)
    {
        package.Validate();
        var approval = package.CurrentApproval;
        if (package.Coordinate != request.SandboxImage || package.Coordinate.Kind != PackageKind.SandboxImage ||
            !package.AllowedTenantIds.Contains(request.Identity.TenantId) || !package.AllowedEnvironments.Contains(request.Environment) ||
            !package.AvailableInSovereignRegistry || approval is null || approval.Status != PackageApprovalStatus.Approved ||
            approval.ExpiresAt <= decidedAt || string.IsNullOrWhiteSpace(approval.EvidenceReference) ||
            string.IsNullOrWhiteSpace(package.SbomReference) || string.IsNullOrWhiteSpace(package.SignatureReference))
            throw new UnauthorizedAccessException("A current supply-chain-complete institutional sandbox image is required.");
    }

    private static void ValidateAssurance(GovernedSandboxExecutionRequest request, PackageSupplyChainAssuranceDecision value)
    {
        if (value.SelectionId != request.ExecutionId || value.Coordinate != request.SandboxImage ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) || !value.DigestVerified ||
            !value.ProvenanceVerified || !value.SbomVerified || !value.SignatureVerified ||
            !value.SovereignRegistryVerified || value.PackageTransferred || value.PackageExecuted ||
            value.ExternalEffectOccurred || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Sandbox image supply-chain assurance denied or performed a forbidden effect.");
    }

    private static void ValidateResult(SandboxExecutionResult value)
    {
        if (!value.IsAccepted || value.ProducedArtifactReferences.IsDefault ||
            value.ProducedArtifactReferences.Any(string.IsNullOrWhiteSpace) ||
            value.ProducedArtifactReferences.Distinct(StringComparer.Ordinal).Count() != value.ProducedArtifactReferences.Length)
            throw new UnauthorizedAccessException("Sandbox result is rejected, incomplete, or contains duplicate artifacts.");
    }

    private static string Digest(GovernedSandboxExecutionRequest request, SandboxPolicyDecision decision, SandboxExecutionResult result)
    {
        var canonical = new StringBuilder().Append(request.ExecutionId.ToString("D")).Append('|')
            .Append(request.ExpectedCandidateSha256Digest.ToLowerInvariant()).Append('|')
            .Append(request.ExpectedSecurityReportSha256Digest.ToLowerInvariant()).Append('|')
            .Append(CoordinateKey(request.SandboxImage)).Append('|')
            .Append(request.IsolationPolicy.IsolationClass).Append(':').Append(request.IsolationPolicy.Ephemeral)
            .Append(':').Append(request.IsolationPolicy.MicroVmIsolation).Append(':')
            .Append(request.IsolationPolicy.ProductionCredentialsAllowed).Append(':')
            .Append(request.IsolationPolicy.HostFilesystemAccessAllowed).Append(':')
            .Append(request.IsolationPolicy.NetworkDefaultDeny).Append(':')
            .Append(request.IsolationPolicy.CpuLimit).Append(':').Append(request.IsolationPolicy.MemoryLimitBytes)
            .Append(':').Append(request.IsolationPolicy.ExecutionTimeout.Ticks).Append(':')
            .AppendJoin(',', request.IsolationPolicy.AllowedNetworkDestinations.Order(StringComparer.Ordinal)).Append('|')
            .AppendJoin(',', request.NonSecretEnvironmentReferences.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => $"{item.Key}={item.Value}")).Append('|')
            .Append(decision.DecisionRequestId.ToString("D")).Append('|').Append(result.ExitCode)
            .Append('|').Append(result.TimedOut).Append('|').Append(result.IsolationViolationDetected)
            .Append('|').AppendJoin(',', result.ProducedArtifactReferences.Order(StringComparer.Ordinal))
            .Append('|').Append(result.EvidenceReference).Append('|')
            .Append(decision.DecidedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }

    private static string CoordinateKey(PackageCoordinate coordinate) =>
        $"{coordinate.Kind}|{coordinate.Name}|{coordinate.Version}|{coordinate.ContentDigest}";

    private static void ValidateAuthorization(SandboxResultAuthorizationRequest request, SandboxResultAuthorizationDecision value)
    {
        if (value.AuthorizationRequestId != request.AuthorizationRequestId || value.ExecutionId != request.ExecutionId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ResultSha256Digest, request.ResultSha256Digest) ||
            !value.IsAllowed || string.IsNullOrWhiteSpace(value.Code) || value.EvidenceReferences.IsDefaultOrEmpty ||
            value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Sandbox result authorization denied or mismatched.");
    }

    private static void ValidateEvidence(SandboxEvidenceRecord record, SandboxEvidenceReceipt value)
    {
        if (value.ExecutionId != record.ExecutionId || value.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ResultSha256Digest, record.ResultSha256Digest) ||
            string.IsNullOrWhiteSpace(value.EvidenceReference) || value.RecordedAt < record.ExecutedAt)
            throw new InvalidOperationException("Sandbox evidence receipt is invalid.");
    }
}
