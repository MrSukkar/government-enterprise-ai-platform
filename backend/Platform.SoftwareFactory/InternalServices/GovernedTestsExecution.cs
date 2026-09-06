using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.Packages;
using Platform.SoftwareFactory.Sandbox;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedTestDefinition(
    string TestId, string Category, string SourceRelativePath, bool Required)
{
    public GovernedTestDefinition Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(TestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Category);
        GovernedGeneratedPath.Validate(SourceRelativePath);
        return this;
    }
}

public sealed record GovernedTestManifest(
    string ManifestReference, string Sha256Digest, ImmutableArray<GovernedTestDefinition> Tests)
{
    public GovernedTestManifest Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ManifestReference);
        GovernedAiPlanningRequest.ValidateDigest(Sha256Digest, "test manifest");
        if (Tests.IsDefaultOrEmpty || Tests.Any(item => item is null) ||
            Tests.GroupBy(item => item.TestId, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new InvalidOperationException("The governed test manifest requires unique tests.");
        foreach (var test in Tests) test.Validate();
        return this;
    }
}

public sealed record GovernedTestsExecutionRequest(
    Guid ExecutionId, Guid SandboxExecutionId, Guid SecurityValidationId, Guid GenerationId, Guid DeliveryRunId,
    string ExpectedCandidateSha256Digest, string ExpectedSecurityReportSha256Digest,
    string ExpectedSandboxResultSha256Digest, string ExpectedSandboxEvidenceReference,
    string TestManifestReference, string ExpectedTestManifestSha256Digest,
    ImmutableHashSet<string> RequiredTestIds, ImmutableHashSet<string> AllowedTestCategories,
    PackageCoordinate TestImage, SandboxIsolationPolicy IsolationPolicy,
    ImmutableDictionary<string, string> NonSecretEnvironmentReferences,
    GovernedIdentity Identity, string Purpose, DataClassification MaximumClassification,
    string AuthorizationEvidenceReference, string Environment,
    IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedTestsExecutionRequest Validate()
    {
        if (ExecutionId == Guid.Empty || SandboxExecutionId == Guid.Empty || SecurityValidationId == Guid.Empty ||
            GenerationId == Guid.Empty || DeliveryRunId == Guid.Empty)
            throw new InvalidOperationException("Tests and prerequisite identities are required.");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedCandidateSha256Digest, "code candidate");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedSecurityReportSha256Digest, "Security report");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedSandboxResultSha256Digest, "Sandbox result");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedTestManifestSha256Digest, "test manifest");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedSandboxEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(TestManifestReference);
        if (RequiredTestIds.IsEmpty || RequiredTestIds.Any(string.IsNullOrWhiteSpace) ||
            AllowedTestCategories.IsEmpty || AllowedTestCategories.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Required test identities and allowed categories are required.");
        TestImage.Validate();
        if (TestImage.Kind != PackageKind.SandboxImage)
            throw new InvalidOperationException("Tests require an exact institutional sandbox image.");
        IsolationPolicy.Validate();
        if (NonSecretEnvironmentReferences is null || NonSecretEnvironmentReferences.Any(item =>
                string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value) ||
                IsSecretLike(item.Key) || IsSecretLike(item.Value)))
            throw new InvalidOperationException("Only non-secret environment references are permitted.");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("developer.internal-service.tests.execute"))
            throw new UnauthorizedAccessException("Governed Tests execution permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Tests execution.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Tests policy environment or time is invalid.");
        return this;
    }

    private static bool IsSecretLike(string value) =>
        value.Contains("SECRET", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("TOKEN", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("CONNECTIONSTRING", StringComparison.OrdinalIgnoreCase);
}

public interface IAuthorizedSandboxExecutionReceiptReader
{
    Task<GovernedSandboxExecutionReceipt?> LoadAsync(Guid executionId, string tenantId, CancellationToken cancellationToken);
}

public interface ITestsDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken);
}

public interface IGovernedTestManifestReader
{
    Task<GovernedTestManifest?> LoadAsync(string manifestReference, string tenantId, CancellationToken cancellationToken);
}

public sealed record TestsPolicyInput(
    Guid DecisionRequestId, Guid ExecutionId, Guid SandboxExecutionId, Guid SecurityValidationId,
    Guid GenerationId, Guid DeliveryRunId, string TenantId, string SubjectId, string Purpose,
    string Environment, DataClassification MaximumClassification, string CandidateSha256Digest,
    string SecurityReportSha256Digest, string SandboxResultSha256Digest, string SandboxEvidenceReference,
    string TestManifestReference, string TestManifestSha256Digest, ImmutableHashSet<string> RequiredTestIds,
    ImmutableHashSet<string> AllowedTestCategories, PackageCoordinate TestImage,
    SandboxIsolationPolicy IsolationPolicy, ImmutableDictionary<string, string> NonSecretEnvironmentReferences,
    IntentPolicyBundleReference PolicyBundle, ImmutableArray<string> EvidenceReferences, DateTimeOffset EvaluatedAt);

public sealed record TestsPolicyDecision(
    Guid DecisionRequestId, Guid ExecutionId, Guid SandboxExecutionId, Guid SecurityValidationId,
    Guid GenerationId, Guid DeliveryRunId, string TenantId, string Environment,
    string CandidateSha256Digest, string SecurityReportSha256Digest, string SandboxResultSha256Digest,
    string TestManifestReference, string TestManifestSha256Digest, ImmutableHashSet<string> RequiredTestIds,
    ImmutableHashSet<string> AllowedTestCategories, PackageCoordinate TestImage,
    SandboxIsolationPolicy IsolationPolicy, ImmutableDictionary<string, string> AllowedEnvironmentReferences,
    ImmutableHashSet<string> AllowedNetworkDestinations, string BundleId, string BundleVersion,
    string BundleSha256Digest, bool PolicySignatureValid, string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome, DataClassification MaximumClassification,
    ImmutableArray<string> Reasons, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ITestsPolicyGate
{
    Task<TestsPolicyDecision> EvaluateAsync(TestsPolicyInput input, CancellationToken cancellationToken);
}

public sealed record GovernedTestRuntimeRequest(
    SoftwareDeliveryRun Run, AiCandidateArtifact Candidate, GovernedTestManifest Manifest,
    PackageCoordinate TestImage, PackageUseDecision TestImageDecision,
    SandboxIsolationPolicy IsolationPolicy,
    ImmutableDictionary<string, string> NonSecretEnvironmentReferences);

public sealed record GovernedTestCaseResult(
    string TestId, string Category, bool Discovered, bool Completed, bool Passed, bool Skipped,
    string EvidenceReference);

public sealed record GovernedTestRuntimeResult(
    bool TimedOut, bool IsolationViolationDetected, ImmutableArray<GovernedTestCaseResult> TestResults,
    string EvidenceReference);

public interface IGovernedTestRuntime
{
    Task<GovernedTestRuntimeResult> ExecuteAsync(GovernedTestRuntimeRequest request, CancellationToken cancellationToken);
}

public sealed record TestsResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid ExecutionId, string TenantId, string SubjectId, string Purpose,
    string Environment, DataClassification MaximumClassification, string CandidateSha256Digest,
    string SecurityReportSha256Digest, string SandboxResultSha256Digest, string TestManifestSha256Digest,
    PackageCoordinate TestImage, SandboxIsolationPolicy IsolationPolicy,
    ImmutableDictionary<string, string> EnvironmentReferences, GovernedTestRuntimeResult Result,
    string ResultSha256Digest, ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record TestsResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid ExecutionId, string TenantId, string ResultSha256Digest,
    bool IsAllowed, string Code, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ITestsResultAuthorizer
{
    Task<TestsResultAuthorizationDecision> AuthorizeAsync(TestsResultAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record TestsEvidenceRecord(
    Guid ExecutionId, Guid SandboxExecutionId, Guid SecurityValidationId, Guid GenerationId,
    Guid DeliveryRunId, string TenantId, Guid PolicyDecisionRequestId, string TestManifestSha256Digest,
    PackageCoordinate TestImage, SandboxIsolationPolicy IsolationPolicy, GovernedTestRuntimeResult Result,
    string ResultSha256Digest, ImmutableArray<string> EvidenceReferences, DateTimeOffset ExecutedAt);

public sealed record TestsEvidenceReceipt(
    Guid ExecutionId, Guid DeliveryRunId, string TenantId, string ResultSha256Digest,
    string EvidenceReference, DateTimeOffset RecordedAt);

public interface ITestsEvidenceRecorder
{
    Task<TestsEvidenceReceipt> RecordAsync(TestsEvidenceRecord record, CancellationToken cancellationToken);
}

public sealed class TestsDependencyUnavailableException(string message) : Exception(message);

public sealed record GovernedTestsExecutionReceipt(
    Guid ExecutionId, Guid SandboxExecutionId, Guid SecurityValidationId, Guid GenerationId,
    Guid DeliveryRunId, string TenantId, GovernedIntentPolicyOutcome PolicyOutcome,
    bool IsAccepted, bool ProductionEffectOccurred, bool CanAdvance,
    string CandidateSha256Digest, string SecurityReportSha256Digest, string SandboxResultSha256Digest,
    string TestManifestSha256Digest, PackageCoordinate TestImage,
    ImmutableArray<GovernedTestCaseResult> TestResults, string? ResultSha256Digest,
    string? TestsEvidenceReference, ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedTestsExecutionEngine(IPackageEligibilityEvaluator eligibilityEvaluator)
{
    public async Task<GovernedTestsExecutionReceipt> ExecuteAsync(
        GovernedTestsExecutionRequest request, ITestsPolicyGate policyGate,
        IAuthorizedSandboxExecutionReceiptReader sandboxReader,
        IAuthorizedSecurityValidationReceiptReader securityReader,
        IAuthorizedCodeGenerationCandidateReader candidateReader, ITestsDeliveryRunReader runReader,
        IGovernedTestManifestReader manifestReader, IInstitutionalPackageRegistryReader registryReader,
        IApprovedPackageSupplyChainVerifier supplyChainVerifier, IGovernedTestRuntime runtime,
        ITestsResultAuthorizer resultAuthorizer, ITestsEvidenceRecorder evidenceRecorder,
        CancellationToken cancellationToken)
    {
        request.Validate();
        var initialEvidence = ImmutableArray.Create(request.ExpectedSandboxEvidenceReference, request.AuthorizationEvidenceReference);
        var input = new TestsPolicyInput(
            Guid.NewGuid(), request.ExecutionId, request.SandboxExecutionId, request.SecurityValidationId,
            request.GenerationId, request.DeliveryRunId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Purpose, request.Environment, request.MaximumClassification,
            request.ExpectedCandidateSha256Digest, request.ExpectedSecurityReportSha256Digest,
            request.ExpectedSandboxResultSha256Digest, request.ExpectedSandboxEvidenceReference,
            request.TestManifestReference, request.ExpectedTestManifestSha256Digest, request.RequiredTestIds,
            request.AllowedTestCategories, request.TestImage, request.IsolationPolicy,
            request.NonSecretEnvironmentReferences, request.PolicyBundle, initialEvidence, request.RequestedAt);
        var decision = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidateDecision(input, request.Identity, decision);
        var policyEvidence = initialEvidence.Append(decision.PolicyVerificationEvidenceReference)
            .Concat(decision.EvidenceReferences).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        if (decision.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedTestsExecutionReceipt(
                request.ExecutionId, request.SandboxExecutionId, request.SecurityValidationId,
                request.GenerationId, request.DeliveryRunId, request.Identity.TenantId,
                decision.Outcome, false, false, false, request.ExpectedCandidateSha256Digest,
                request.ExpectedSecurityReportSha256Digest, request.ExpectedSandboxResultSha256Digest,
                request.ExpectedTestManifestSha256Digest, request.TestImage, [], null, null, policyEvidence,
                "Policy denial requires a new governed Tests request", decision.DecidedAt);

        var sandbox = await sandboxReader.LoadAsync(request.SandboxExecutionId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Sandbox receipt was not found.");
        ValidateSandbox(request, sandbox);
        var security = await securityReader.LoadAsync(request.SecurityValidationId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Security Validation receipt was not found.");
        ValidateSecurity(request, security);
        var candidate = await candidateReader.LoadAsync(request.GenerationId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Code Generation candidate was not found.");
        ValidateCandidate(request, candidate);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);
        var manifest = await manifestReader.LoadAsync(request.TestManifestReference, request.Identity.TenantId, cancellationToken)
            ?? throw new TestsDependencyUnavailableException("The governed test manifest is unavailable.");
        ValidateManifest(request, manifest);

        var package = await registryReader.FindExactAsync(request.TestImage, cancellationToken)
            ?? throw new TestsDependencyUnavailableException("The exact authorized test image is unavailable.");
        ValidatePackage(request, package, decision.DecidedAt);
        var eligibility = eligibilityEvaluator.Evaluate(package,
            new PackageUseRequest(request.TestImage, request.Identity.TenantId, request.Environment, decision.DecidedAt));
        if (!eligibility.IsAllowed)
            throw new UnauthorizedAccessException($"Test image eligibility denied: {eligibility.Code}.");
        var assurance = await supplyChainVerifier.VerifyAsync(new PackageSupplyChainAssuranceRequest(
            request.ExecutionId, request.Identity.TenantId, request.Environment, package,
            policyEvidence, decision.DecidedAt), cancellationToken);
        ValidateAssurance(request, assurance);

        var result = await runtime.ExecuteAsync(new GovernedTestRuntimeRequest(
            run, candidate.Candidate, manifest, request.TestImage, eligibility,
            request.IsolationPolicy, request.NonSecretEnvironmentReferences), cancellationToken);
        ValidateResult(manifest, result);
        var resultDigest = Digest(request, decision, manifest, result);
        var resultEvidence = policyEvidence.Concat(sandbox.EvidenceReferences)
            .Append(sandbox.ExecutionEvidenceReference!).Concat(security.EvidenceReferences)
            .Append(security.ValidationEvidenceReference!).Concat(candidate.Receipt.EvidenceReferences)
            .Append(candidate.Receipt.GenerationEvidenceReference!).Append(run.History[^1].EvidenceReference)
            .Concat(assurance.EvidenceReferences).Append(result.EvidenceReference)
            .Concat(result.TestResults.Select(item => item.EvidenceReference))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var authorizationRequest = new TestsResultAuthorizationRequest(
            Guid.NewGuid(), request.ExecutionId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Purpose, request.Environment, request.MaximumClassification,
            request.ExpectedCandidateSha256Digest, request.ExpectedSecurityReportSha256Digest,
            request.ExpectedSandboxResultSha256Digest, request.ExpectedTestManifestSha256Digest,
            request.TestImage, request.IsolationPolicy, request.NonSecretEnvironmentReferences,
            result, resultDigest, resultEvidence, assurance.DecidedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
        ValidateAuthorization(authorizationRequest, authorization);
        var allEvidence = resultEvidence.Concat(authorization.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new TestsEvidenceRecord(
            request.ExecutionId, request.SandboxExecutionId, request.SecurityValidationId,
            request.GenerationId, request.DeliveryRunId, request.Identity.TenantId,
            decision.DecisionRequestId, request.ExpectedTestManifestSha256Digest, request.TestImage,
            request.IsolationPolicy, result, resultDigest, allEvidence, authorization.DecidedAt);
        var evidence = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidence(record, evidence);
        return new GovernedTestsExecutionReceipt(
            request.ExecutionId, request.SandboxExecutionId, request.SecurityValidationId,
            request.GenerationId, request.DeliveryRunId, request.Identity.TenantId,
            decision.Outcome, true, false, false, request.ExpectedCandidateSha256Digest,
            request.ExpectedSecurityReportSha256Digest, request.ExpectedSandboxResultSha256Digest,
            request.ExpectedTestManifestSha256Digest, request.TestImage,
            result.TestResults.OrderBy(item => item.TestId, StringComparer.Ordinal).ToImmutableArray(),
            resultDigest, evidence.EvidenceReference,
            allEvidence.Append(evidence.EvidenceReference).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved Human Review", evidence.RecordedAt);
    }

    private static void ValidateDecision(TestsPolicyInput input, GovernedIdentity identity, TestsPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.ExecutionId != input.ExecutionId ||
            value.SandboxExecutionId != input.SandboxExecutionId || value.SecurityValidationId != input.SecurityValidationId ||
            value.GenerationId != input.GenerationId || value.DeliveryRunId != input.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, input.TenantId) ||
            !StringComparer.Ordinal.Equals(value.Environment, input.Environment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, input.CandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SecurityReportSha256Digest, input.SecurityReportSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SandboxResultSha256Digest, input.SandboxResultSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.TestManifestReference, input.TestManifestReference) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.TestManifestSha256Digest, input.TestManifestSha256Digest) ||
            !value.RequiredTestIds.SetEquals(input.RequiredTestIds) ||
            !value.AllowedTestCategories.SetEquals(input.AllowedTestCategories) || value.TestImage != input.TestImage ||
            !IsolationMatches(value.IsolationPolicy, input.IsolationPolicy) ||
            value.AllowedEnvironmentReferences.Count != input.NonSecretEnvironmentReferences.Count ||
            value.AllowedEnvironmentReferences.Any(item => !input.NonSecretEnvironmentReferences.TryGetValue(item.Key, out var expected) ||
                !StringComparer.Ordinal.Equals(item.Value, expected)) ||
            !value.AllowedNetworkDestinations.SetEquals(input.IsolationPolicy.AllowedNetworkDestinations) ||
            !StringComparer.Ordinal.Equals(value.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(value.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Tests decision.");
        if (!value.PolicySignatureValid || string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) ||
            value.MaximumClassification > input.MaximumClassification || value.MaximumClassification > identity.Clearance ||
            value.Reasons.IsDefaultOrEmpty || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("Tests OPA decision is invalid.");
    }

    private static bool IsolationMatches(SandboxIsolationPolicy left, SandboxIsolationPolicy right) =>
        StringComparer.Ordinal.Equals(left.IsolationClass, right.IsolationClass) && left.Ephemeral == right.Ephemeral &&
        left.MicroVmIsolation == right.MicroVmIsolation &&
        left.ProductionCredentialsAllowed == right.ProductionCredentialsAllowed &&
        left.HostFilesystemAccessAllowed == right.HostFilesystemAccessAllowed &&
        left.NetworkDefaultDeny == right.NetworkDefaultDeny && left.CpuLimit == right.CpuLimit &&
        left.MemoryLimitBytes == right.MemoryLimitBytes && left.ExecutionTimeout == right.ExecutionTimeout &&
        left.AllowedNetworkDestinations.SetEquals(right.AllowedNetworkDestinations);

    private static void ValidateSandbox(GovernedTestsExecutionRequest request, GovernedSandboxExecutionReceipt value)
    {
        if (value.ExecutionId != request.SandboxExecutionId || value.SecurityValidationId != request.SecurityValidationId ||
            value.GenerationId != request.GenerationId || value.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SecurityReportSha256Digest, request.ExpectedSecurityReportSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ResultSha256Digest, request.ExpectedSandboxResultSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.ExecutionEvidenceReference, request.ExpectedSandboxEvidenceReference) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted ||
            value.ProductionEffectOccurred || value.CanAdvance || value.ExitCode != 0 || value.TimedOut != false ||
            value.IsolationViolationDetected != false || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Sandbox prerequisite is invalid or mismatched.");
    }

    private static void ValidateSecurity(GovernedTestsExecutionRequest request, GovernedSecurityValidationReceipt value)
    {
        if (value.ValidationId != request.SecurityValidationId || value.GenerationId != request.GenerationId ||
            value.DeliveryRunId != request.DeliveryRunId || !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SecurityReportSha256Digest, request.ExpectedSecurityReportSha256Digest) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted ||
            value.IsExecutable || value.CanAdvance || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Security prerequisite is invalid or mismatched.");
    }

    private static void ValidateCandidate(GovernedTestsExecutionRequest request, AuthorizedCodeGenerationCandidateSnapshot value)
    {
        value.Candidate.Validate();
        if (value.Receipt.GenerationId != request.GenerationId || value.Receipt.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.Receipt.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.Receipt.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            value.Receipt.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.Receipt.IsCodeCandidateReleased ||
            value.Receipt.IsExecutable || value.Receipt.IsApplied || value.Receipt.CanAdvance ||
            !StringComparer.Ordinal.Equals(value.Receipt.Content, value.Candidate.Content) || value.Receipt.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Code candidate is invalid or mismatched.");
    }

    private static void ValidateRun(GovernedTestsExecutionRequest request, SoftwareDeliveryRun run)
    {
        run.Validate();
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.Sandbox).ToArray();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.Sandbox || run.History.Length != expected.Length ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) || index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run is not stopped at a valid Sandbox boundary.");
    }

    private static void ValidateManifest(GovernedTestsExecutionRequest request, GovernedTestManifest manifest)
    {
        manifest.Validate();
        var requiredIds = manifest.Tests.Where(item => item.Required).Select(item => item.TestId)
            .ToImmutableHashSet(StringComparer.Ordinal);
        if (!StringComparer.Ordinal.Equals(manifest.ManifestReference, request.TestManifestReference) ||
            !StringComparer.OrdinalIgnoreCase.Equals(manifest.Sha256Digest, request.ExpectedTestManifestSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(manifest.Sha256Digest, ManifestDigest(manifest)) ||
            !requiredIds.SetEquals(request.RequiredTestIds) ||
            manifest.Tests.Any(item => !request.AllowedTestCategories.Contains(item.Category)))
            throw new UnauthorizedAccessException("Governed test manifest is mismatched or outside policy scope.");
    }

    private static void ValidatePackage(GovernedTestsExecutionRequest request, InstitutionalPackage package, DateTimeOffset decidedAt)
    {
        package.Validate();
        var approval = package.CurrentApproval;
        if (package.Coordinate != request.TestImage || package.Coordinate.Kind != PackageKind.SandboxImage ||
            !package.AllowedTenantIds.Contains(request.Identity.TenantId) || !package.AllowedEnvironments.Contains(request.Environment) ||
            !package.AvailableInSovereignRegistry || approval is null || approval.Status != PackageApprovalStatus.Approved ||
            approval.ExpiresAt <= decidedAt || string.IsNullOrWhiteSpace(approval.EvidenceReference) ||
            string.IsNullOrWhiteSpace(package.SbomReference) || string.IsNullOrWhiteSpace(package.SignatureReference))
            throw new UnauthorizedAccessException("A current supply-chain-complete institutional test image is required.");
    }

    private static void ValidateAssurance(GovernedTestsExecutionRequest request, PackageSupplyChainAssuranceDecision value)
    {
        if (value.SelectionId != request.ExecutionId || value.Coordinate != request.TestImage ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) || !value.DigestVerified ||
            !value.ProvenanceVerified || !value.SbomVerified || !value.SignatureVerified ||
            !value.SovereignRegistryVerified || value.PackageTransferred || value.PackageExecuted ||
            value.ExternalEffectOccurred || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Test image assurance denied or performed a forbidden effect.");
    }

    private static void ValidateResult(GovernedTestManifest manifest, GovernedTestRuntimeResult value)
    {
        if (value.TimedOut || value.IsolationViolationDetected || string.IsNullOrWhiteSpace(value.EvidenceReference) ||
            value.TestResults.IsDefaultOrEmpty || value.TestResults.GroupBy(item => item.TestId, StringComparer.Ordinal).Any(group => group.Count() != 1) ||
            !value.TestResults.Select(item => item.TestId).ToImmutableHashSet(StringComparer.Ordinal)
                .SetEquals(manifest.Tests.Select(item => item.TestId)) ||
            value.TestResults.Any(item => string.IsNullOrWhiteSpace(item.TestId) || string.IsNullOrWhiteSpace(item.Category) ||
                !item.Discovered || string.IsNullOrWhiteSpace(item.EvidenceReference)) ||
            value.TestResults.Any(result => manifest.Tests.All(test => test.TestId != result.TestId ||
                !StringComparer.Ordinal.Equals(test.Category, result.Category))) ||
            value.TestResults.Any(result => manifest.Tests.Any(test => test.TestId == result.TestId &&
                (test.Required && (!result.Completed || !result.Passed || result.Skipped) ||
                 !test.Required && !result.Skipped && !result.Completed))))
            throw new UnauthorizedAccessException("Tests result is incomplete, failed, skipped, or invalid.");
    }

    private static string ManifestDigest(GovernedTestManifest manifest)
    {
        var canonical = new StringBuilder().Append(manifest.ManifestReference);
        foreach (var test in manifest.Tests.OrderBy(item => item.TestId, StringComparer.Ordinal))
            canonical.Append('|').Append(test.TestId).Append(':').Append(test.Category).Append(':')
                .Append(test.SourceRelativePath).Append(':').Append(test.Required);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }

    private static string Digest(GovernedTestsExecutionRequest request, TestsPolicyDecision decision,
        GovernedTestManifest manifest, GovernedTestRuntimeResult result)
    {
        var canonical = new StringBuilder().Append(request.ExecutionId.ToString("D")).Append('|')
            .Append(request.ExpectedCandidateSha256Digest.ToLowerInvariant()).Append('|')
            .Append(request.ExpectedSecurityReportSha256Digest.ToLowerInvariant()).Append('|')
            .Append(request.ExpectedSandboxResultSha256Digest.ToLowerInvariant()).Append('|')
            .Append(manifest.Sha256Digest.ToLowerInvariant()).Append('|').Append(CoordinateKey(request.TestImage)).Append('|')
            .Append(request.IsolationPolicy.IsolationClass).Append(':').Append(request.IsolationPolicy.Ephemeral).Append(':')
            .Append(request.IsolationPolicy.MicroVmIsolation).Append(':')
            .Append(request.IsolationPolicy.ProductionCredentialsAllowed).Append(':')
            .Append(request.IsolationPolicy.HostFilesystemAccessAllowed).Append(':')
            .Append(request.IsolationPolicy.NetworkDefaultDeny).Append(':').Append(request.IsolationPolicy.CpuLimit).Append(':')
            .Append(request.IsolationPolicy.MemoryLimitBytes).Append(':').Append(request.IsolationPolicy.ExecutionTimeout.Ticks).Append(':')
            .AppendJoin(',', request.IsolationPolicy.AllowedNetworkDestinations.Order(StringComparer.Ordinal)).Append('|')
            .AppendJoin(',', request.NonSecretEnvironmentReferences.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => $"{item.Key}={item.Value}")).Append('|')
            .Append(decision.DecisionRequestId.ToString("D")).Append('|').Append(result.TimedOut).Append('|')
            .Append(result.IsolationViolationDetected).Append('|')
            .AppendJoin(',', result.TestResults.OrderBy(item => item.TestId, StringComparer.Ordinal).Select(item =>
                $"{item.TestId}:{item.Category}:{item.Discovered}:{item.Completed}:{item.Passed}:{item.Skipped}:{item.EvidenceReference}"))
            .Append('|').Append(result.EvidenceReference).Append('|')
            .Append(decision.DecidedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }

    private static string CoordinateKey(PackageCoordinate coordinate) =>
        $"{coordinate.Kind}|{coordinate.Name}|{coordinate.Version}|{coordinate.ContentDigest}";

    private static void ValidateAuthorization(TestsResultAuthorizationRequest request, TestsResultAuthorizationDecision value)
    {
        if (value.AuthorizationRequestId != request.AuthorizationRequestId || value.ExecutionId != request.ExecutionId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ResultSha256Digest, request.ResultSha256Digest) ||
            !value.IsAllowed || string.IsNullOrWhiteSpace(value.Code) || value.EvidenceReferences.IsDefaultOrEmpty ||
            value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Tests result authorization denied or mismatched.");
    }

    private static void ValidateEvidence(TestsEvidenceRecord record, TestsEvidenceReceipt value)
    {
        if (value.ExecutionId != record.ExecutionId || value.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ResultSha256Digest, record.ResultSha256Digest) ||
            string.IsNullOrWhiteSpace(value.EvidenceReference) || value.RecordedAt < record.ExecutedAt)
            throw new InvalidOperationException("Tests evidence receipt is invalid.");
    }
}
