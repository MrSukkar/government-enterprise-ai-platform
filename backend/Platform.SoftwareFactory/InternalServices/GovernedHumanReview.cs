using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.InternalService;

public enum HumanReviewDecision
{
    Approve,
    Reject
}

public sealed record GovernedHumanReviewRequest(
    Guid ReviewId, Guid TestsExecutionId, Guid SandboxExecutionId, Guid SecurityValidationId,
    Guid GenerationId, Guid DeliveryRunId, string InitiatorSubjectId, string ExpectedCandidateSha256Digest,
    string ExpectedSecurityReportSha256Digest, string ExpectedSandboxResultSha256Digest,
    string ExpectedTestsResultSha256Digest, string ExpectedTestsEvidenceReference,
    HumanReviewDecision Decision, string Rationale, string HumanAttestationReference,
    ImmutableHashSet<string> DeclaredConflictingSubjectIds, long ExpectedVersion,
    GovernedIdentity Identity, string Purpose, DataClassification MaximumClassification,
    string AuthorizationEvidenceReference, string Environment,
    IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedHumanReviewRequest Validate()
    {
        if (ReviewId == Guid.Empty || TestsExecutionId == Guid.Empty || SandboxExecutionId == Guid.Empty ||
            SecurityValidationId == Guid.Empty || GenerationId == Guid.Empty || DeliveryRunId == Guid.Empty)
            throw new InvalidOperationException("Human Review and prerequisite identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(InitiatorSubjectId);
        GovernedAiPlanningRequest.ValidateDigest(ExpectedCandidateSha256Digest, "code candidate");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedSecurityReportSha256Digest, "Security report");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedSandboxResultSha256Digest, "Sandbox result");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedTestsResultSha256Digest, "Tests result");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedTestsEvidenceReference);
        if (!Enum.IsDefined(Decision)) throw new InvalidOperationException("An explicit human review decision is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Rationale);
        ArgumentException.ThrowIfNullOrWhiteSpace(HumanAttestationReference);
        if (DeclaredConflictingSubjectIds.IsEmpty || DeclaredConflictingSubjectIds.Any(string.IsNullOrWhiteSpace) ||
            !DeclaredConflictingSubjectIds.Contains(InitiatorSubjectId))
            throw new InvalidOperationException("The review conflict scope is required.");
        if (ExpectedVersion < -1) throw new ArgumentOutOfRangeException(nameof(ExpectedVersion));
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("developer.internal-service.human-review.decide"))
            throw new UnauthorizedAccessException("Governed Human Review permission is required.");
        if (DeclaredConflictingSubjectIds.Contains(Identity.SubjectId))
            throw new UnauthorizedAccessException("A conflicting actor cannot perform Human Review.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Human Review.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Human Review policy environment or time is invalid.");
        return this;
    }
}

public interface IAuthorizedTestsExecutionReceiptReader
{
    Task<GovernedTestsExecutionReceipt?> LoadAsync(Guid executionId, string tenantId, CancellationToken cancellationToken);
}

public interface IHumanReviewTestsReceiptReader
{
    Task<GovernedTestsExecutionReceipt?> LoadAsync(Guid executionId, string tenantId, string purpose,
        Guid sandboxExecutionId, Guid securityValidationId, Guid generationId, Guid deliveryRunId,
        string candidateSha256Digest, string securityReportSha256Digest, string sandboxResultSha256Digest,
        string testsResultSha256Digest, string testsEvidenceReference, CancellationToken cancellationToken);
}

public interface IHumanReviewSandboxReceiptReader
{
    Task<GovernedSandboxExecutionReceipt?> LoadAsync(Guid executionId, string tenantId, string purpose,
        Guid securityValidationId, Guid generationId, Guid deliveryRunId, string candidateSha256Digest,
        string securityReportSha256Digest, string resultSha256Digest, CancellationToken cancellationToken);
}

public interface IHumanReviewSecurityReceiptReader
{
    Task<GovernedSecurityValidationReceipt?> LoadAsync(Guid validationId, string tenantId, string purpose,
        Guid generationId, Guid deliveryRunId, string candidateSha256Digest,
        string securityReportSha256Digest, CancellationToken cancellationToken);
}

public interface IHumanReviewCandidateReader
{
    Task<AuthorizedCodeGenerationCandidateSnapshot?> LoadAsync(Guid generationId, string tenantId,
        string purpose, Guid deliveryRunId, string candidateSha256Digest, CancellationToken cancellationToken);
}

public interface IHumanReviewDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, string purpose,
        Guid testsExecutionId, Guid sandboxExecutionId, Guid generationId,
        string candidateSha256Digest, string testsResultSha256Digest, CancellationToken cancellationToken);
}

public sealed record HumanReviewPolicyInput(
    Guid DecisionRequestId, Guid ReviewId, Guid TestsExecutionId, Guid SandboxExecutionId,
    Guid SecurityValidationId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, string ReviewerSubjectId, string InitiatorSubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, string CandidateSha256Digest,
    string SecurityReportSha256Digest, string SandboxResultSha256Digest,
    string TestsResultSha256Digest, string TestsEvidenceReference,
    HumanReviewDecision HumanDecision, string Rationale, string HumanAttestationReference,
    ImmutableHashSet<string> DeclaredConflictingSubjectIds, long ExpectedVersion,
    IntentPolicyBundleReference PolicyBundle, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset EvaluatedAt);

public sealed record HumanReviewPolicyDecision(
    Guid DecisionRequestId, Guid ReviewId, Guid TestsExecutionId, Guid SandboxExecutionId,
    Guid SecurityValidationId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, string ReviewerSubjectId, string InitiatorSubjectId, string Environment,
    string CandidateSha256Digest, string SecurityReportSha256Digest,
    string SandboxResultSha256Digest, string TestsResultSha256Digest,
    HumanReviewDecision HumanDecision, string Rationale, string HumanAttestationReference,
    ImmutableHashSet<string> ConflictingSubjectIds, bool ReviewerIsHuman,
    string BundleId, string BundleVersion, string BundleSha256Digest,
    bool PolicySignatureValid, string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome, DataClassification MaximumClassification,
    ImmutableArray<string> Reasons, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IHumanReviewPolicyGate
{
    Task<HumanReviewPolicyDecision> EvaluateAsync(HumanReviewPolicyInput input, CancellationToken cancellationToken);
}

public sealed record HumanReviewAttestationRequest(
    Guid ReviewId, string TenantId, string ReviewerSubjectId, HumanReviewDecision Decision,
    string Rationale, string ReviewPackageSha256Digest, string HumanAttestationReference,
    string Purpose, string Environment, DataClassification MaximumClassification,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record HumanReviewAttestationDecision(
    Guid ReviewId, string TenantId, string ReviewerSubjectId, HumanReviewDecision Decision,
    string ReviewPackageSha256Digest, string HumanAttestationReference,
    bool IsHumanIdentityVerified, bool IsSignatureValid, bool IsNonRepudiable,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset VerifiedAt);

public interface IHumanReviewAttestationVerifier
{
    Task<HumanReviewAttestationDecision> VerifyAsync(HumanReviewAttestationRequest request, CancellationToken cancellationToken);
}

public sealed record HumanReviewAtomicRecord(
    Guid ReviewId, Guid TestsExecutionId, Guid SandboxExecutionId, Guid SecurityValidationId,
    Guid GenerationId, Guid DeliveryRunId, string TenantId, string ReviewerSubjectId,
    HumanReviewDecision Decision, string Rationale, string HumanAttestationReference,
    string ReviewPackageSha256Digest, Guid PolicyDecisionRequestId, string PolicyBundleSha256Digest,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset ReviewedAt);

public sealed record HumanReviewAtomicReceipt(
    Guid ReviewId, Guid DeliveryRunId, string TenantId, string ReviewerSubjectId,
    HumanReviewDecision Decision, string Rationale, string HumanAttestationReference,
    string ReviewPackageSha256Digest, long Version, string EvidenceReference,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RecordedAt);

public interface IAtomicHumanReviewRepository
{
    Task<HumanReviewAtomicReceipt> RecordDecisionAndEvidenceAsync(
        HumanReviewAtomicRecord record, long expectedVersion, CancellationToken cancellationToken);
}

public sealed class HumanReviewDependencyUnavailableException(string message) : Exception(message);

public sealed record GovernedHumanReviewReceipt(
    Guid ReviewId, Guid TestsExecutionId, Guid SandboxExecutionId, Guid SecurityValidationId,
    Guid GenerationId, Guid DeliveryRunId, string TenantId, string ReviewerSubjectId,
    GovernedIntentPolicyOutcome PolicyOutcome, bool IsRecorded, bool IsApproved,
    bool ProductionEffectOccurred, bool CanAdvance, HumanReviewDecision? Decision,
    string? Rationale, string CandidateSha256Digest, string SecurityReportSha256Digest,
    string SandboxResultSha256Digest, string TestsResultSha256Digest,
    string? ReviewPackageSha256Digest, long? Version, string? ReviewEvidenceReference,
    ImmutableArray<string> EvidenceReferences, string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedHumanReviewEngine
{
    public async Task<GovernedHumanReviewReceipt> ReviewAsync(
        GovernedHumanReviewRequest request, IHumanReviewPolicyGate policyGate,
        IHumanReviewTestsReceiptReader testsReader,
        IHumanReviewSandboxReceiptReader sandboxReader,
        IHumanReviewSecurityReceiptReader securityReader,
        IHumanReviewCandidateReader candidateReader,
        IHumanReviewDeliveryRunReader runReader,
        IHumanReviewAttestationVerifier attestationVerifier,
        IAtomicHumanReviewRepository repository, CancellationToken cancellationToken)
    {
        request.Validate();
        var initialEvidence = ImmutableArray.Create(request.ExpectedTestsEvidenceReference, request.AuthorizationEvidenceReference);
        var input = new HumanReviewPolicyInput(
            Guid.NewGuid(), request.ReviewId, request.TestsExecutionId, request.SandboxExecutionId,
            request.SecurityValidationId, request.GenerationId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, request.InitiatorSubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, request.ExpectedCandidateSha256Digest,
            request.ExpectedSecurityReportSha256Digest, request.ExpectedSandboxResultSha256Digest,
            request.ExpectedTestsResultSha256Digest, request.ExpectedTestsEvidenceReference,
            request.Decision, request.Rationale, request.HumanAttestationReference,
            request.DeclaredConflictingSubjectIds, request.ExpectedVersion, request.PolicyBundle,
            initialEvidence, request.RequestedAt);
        var policy = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidatePolicy(input, request.Identity, policy);
        var policyEvidence = initialEvidence.Append(policy.PolicyVerificationEvidenceReference)
            .Concat(policy.EvidenceReferences).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        if (policy.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedHumanReviewReceipt(
                request.ReviewId, request.TestsExecutionId, request.SandboxExecutionId,
                request.SecurityValidationId, request.GenerationId, request.DeliveryRunId,
                request.Identity.TenantId, request.Identity.SubjectId, policy.Outcome,
                false, false, false, false, null, null, request.ExpectedCandidateSha256Digest,
                request.ExpectedSecurityReportSha256Digest, request.ExpectedSandboxResultSha256Digest,
                request.ExpectedTestsResultSha256Digest, null, null, null, policyEvidence,
                "Policy denial requires a new governed Human Review request", policy.DecidedAt);

        var tests = await testsReader.LoadAsync(request.TestsExecutionId, request.Identity.TenantId, request.Purpose,
            request.SandboxExecutionId, request.SecurityValidationId, request.GenerationId, request.DeliveryRunId,
            request.ExpectedCandidateSha256Digest, request.ExpectedSecurityReportSha256Digest,
            request.ExpectedSandboxResultSha256Digest, request.ExpectedTestsResultSha256Digest,
            request.ExpectedTestsEvidenceReference, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Tests receipt was not found.");
        ValidateTests(request, tests);
        var sandbox = await sandboxReader.LoadAsync(request.SandboxExecutionId, request.Identity.TenantId, request.Purpose,
            request.SecurityValidationId, request.GenerationId, request.DeliveryRunId,
            request.ExpectedCandidateSha256Digest, request.ExpectedSecurityReportSha256Digest,
            request.ExpectedSandboxResultSha256Digest, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Sandbox receipt was not found.");
        ValidateSandbox(request, sandbox);
        var security = await securityReader.LoadAsync(request.SecurityValidationId, request.Identity.TenantId,
            request.Purpose, request.GenerationId, request.DeliveryRunId, request.ExpectedCandidateSha256Digest,
            request.ExpectedSecurityReportSha256Digest, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Security receipt was not found.");
        ValidateSecurity(request, security);
        var candidate = await candidateReader.LoadAsync(request.GenerationId, request.Identity.TenantId,
            request.Purpose, request.DeliveryRunId, request.ExpectedCandidateSha256Digest, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Code Generation candidate was not found.");
        ValidateCandidate(request, candidate);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, request.Purpose,
            request.TestsExecutionId, request.SandboxExecutionId, request.GenerationId,
            request.ExpectedCandidateSha256Digest, request.ExpectedTestsResultSha256Digest, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run, policy);

        var reviewPackageDigest = Digest(request, policy, tests, sandbox, security, candidate);
        var prerequisiteEvidence = policyEvidence.Concat(tests.EvidenceReferences).Append(tests.TestsEvidenceReference!)
            .Concat(sandbox.EvidenceReferences).Append(sandbox.ExecutionEvidenceReference!)
            .Concat(security.EvidenceReferences).Append(security.ValidationEvidenceReference!)
            .Concat(candidate.Receipt.EvidenceReferences).Append(candidate.Receipt.GenerationEvidenceReference!)
            .Append(run.History[^1].EvidenceReference).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        var attestationRequest = new HumanReviewAttestationRequest(
            request.ReviewId, request.Identity.TenantId, request.Identity.SubjectId, request.Decision,
            request.Rationale, reviewPackageDigest, request.HumanAttestationReference,
            request.Purpose, request.Environment, request.MaximumClassification,
            prerequisiteEvidence, policy.DecidedAt);
        var attestation = await attestationVerifier.VerifyAsync(attestationRequest, cancellationToken);
        ValidateAttestation(attestationRequest, attestation);
        var allEvidence = prerequisiteEvidence.Concat(attestation.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new HumanReviewAtomicRecord(
            request.ReviewId, request.TestsExecutionId, request.SandboxExecutionId,
            request.SecurityValidationId, request.GenerationId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Decision,
            request.Rationale, request.HumanAttestationReference, reviewPackageDigest,
            policy.DecisionRequestId, policy.BundleSha256Digest, allEvidence, attestation.VerifiedAt);
        var persisted = await repository.RecordDecisionAndEvidenceAsync(
            record, request.ExpectedVersion, cancellationToken);
        ValidatePersisted(record, request.ExpectedVersion, persisted);
        return new GovernedHumanReviewReceipt(
            request.ReviewId, request.TestsExecutionId, request.SandboxExecutionId,
            request.SecurityValidationId, request.GenerationId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, policy.Outcome,
            true, request.Decision == HumanReviewDecision.Approve, false, false,
            request.Decision, request.Rationale, request.ExpectedCandidateSha256Digest,
            request.ExpectedSecurityReportSha256Digest, request.ExpectedSandboxResultSha256Digest,
            request.ExpectedTestsResultSha256Digest, reviewPackageDigest, persisted.Version,
            persisted.EvidenceReference,
            allEvidence.Concat(persisted.EvidenceReferences).Append(persisted.EvidenceReference)
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
            request.Decision == HumanReviewDecision.Approve
                ? "Separately approved Git boundary" : "Rejected; a new governed change is required",
            persisted.RecordedAt);
    }

    private static void ValidatePolicy(HumanReviewPolicyInput input, GovernedIdentity identity, HumanReviewPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.ReviewId != input.ReviewId ||
            value.TestsExecutionId != input.TestsExecutionId || value.SandboxExecutionId != input.SandboxExecutionId ||
            value.SecurityValidationId != input.SecurityValidationId || value.GenerationId != input.GenerationId ||
            value.DeliveryRunId != input.DeliveryRunId || !StringComparer.Ordinal.Equals(value.TenantId, input.TenantId) ||
            !StringComparer.Ordinal.Equals(value.ReviewerSubjectId, input.ReviewerSubjectId) ||
            !StringComparer.Ordinal.Equals(value.InitiatorSubjectId, input.InitiatorSubjectId) ||
            !StringComparer.Ordinal.Equals(value.Environment, input.Environment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, input.CandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SecurityReportSha256Digest, input.SecurityReportSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SandboxResultSha256Digest, input.SandboxResultSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.TestsResultSha256Digest, input.TestsResultSha256Digest) ||
            value.HumanDecision != input.HumanDecision || !StringComparer.Ordinal.Equals(value.Rationale, input.Rationale) ||
            !StringComparer.Ordinal.Equals(value.HumanAttestationReference, input.HumanAttestationReference) ||
            !value.ConflictingSubjectIds.SetEquals(input.DeclaredConflictingSubjectIds) ||
            !StringComparer.Ordinal.Equals(value.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(value.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Human Review decision.");
        if (!value.PolicySignatureValid || !value.ReviewerIsHuman ||
            string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) ||
            value.ConflictingSubjectIds.Contains(identity.SubjectId) ||
            value.MaximumClassification > input.MaximumClassification || value.MaximumClassification > identity.Clearance ||
            value.Reasons.IsDefaultOrEmpty || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("Human Review OPA decision is invalid.");
    }

    private static void ValidateTests(GovernedHumanReviewRequest request, GovernedTestsExecutionReceipt value)
    {
        if (value.ExecutionId != request.TestsExecutionId || value.SandboxExecutionId != request.SandboxExecutionId ||
            value.SecurityValidationId != request.SecurityValidationId || value.GenerationId != request.GenerationId ||
            value.DeliveryRunId != request.DeliveryRunId || !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SecurityReportSha256Digest, request.ExpectedSecurityReportSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SandboxResultSha256Digest, request.ExpectedSandboxResultSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ResultSha256Digest, request.ExpectedTestsResultSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.TestsEvidenceReference, request.ExpectedTestsEvidenceReference) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted || !value.IsRecordedEquivalent() ||
            value.ProductionEffectOccurred || value.CanAdvance || value.TestResults.IsDefaultOrEmpty || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Tests prerequisite is invalid or mismatched.");
    }

    private static void ValidateSandbox(GovernedHumanReviewRequest request, GovernedSandboxExecutionReceipt value)
    {
        if (value.ExecutionId != request.SandboxExecutionId || value.SecurityValidationId != request.SecurityValidationId ||
            value.GenerationId != request.GenerationId || value.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SecurityReportSha256Digest, request.ExpectedSecurityReportSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ResultSha256Digest, request.ExpectedSandboxResultSha256Digest) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted || value.ExitCode != 0 ||
            value.TimedOut != false || value.IsolationViolationDetected != false ||
            value.ProductionEffectOccurred || value.CanAdvance || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Sandbox prerequisite is invalid or mismatched.");
    }

    private static void ValidateSecurity(GovernedHumanReviewRequest request, GovernedSecurityValidationReceipt value)
    {
        if (value.ValidationId != request.SecurityValidationId || value.GenerationId != request.GenerationId ||
            value.DeliveryRunId != request.DeliveryRunId || !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SecurityReportSha256Digest, request.ExpectedSecurityReportSha256Digest) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted ||
            value.IsExecutable || value.CanAdvance || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Security prerequisite is invalid or mismatched.");
    }

    private static void ValidateCandidate(GovernedHumanReviewRequest request, AuthorizedCodeGenerationCandidateSnapshot value)
    {
        value.Candidate.Validate();
        if (value.Receipt.GenerationId != request.GenerationId || value.Receipt.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.Receipt.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.Receipt.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            value.Receipt.PolicyOutcome != GovernedIntentPolicyOutcome.Permit ||
            !value.Receipt.IsCodeCandidateReleased || value.Receipt.IsExecutable || value.Receipt.IsApplied ||
            value.Receipt.CanAdvance || !StringComparer.Ordinal.Equals(value.Receipt.Content, value.Candidate.Content) ||
            value.Receipt.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Code candidate is invalid or mismatched.");
    }

    private static void ValidateRun(GovernedHumanReviewRequest request, SoftwareDeliveryRun run, HumanReviewPolicyDecision policy)
    {
        run.Validate();
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.Tests).ToArray();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.Tests || run.History.Length != expected.Length ||
            !StringComparer.Ordinal.Equals(run.InitiatorSubjectId, request.InitiatorSubjectId) ||
            !policy.ConflictingSubjectIds.Contains(run.InitiatorSubjectId) ||
            StringComparer.Ordinal.Equals(run.InitiatorSubjectId, request.Identity.SubjectId) ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) || index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run or Human Review separation of duties is invalid.");
    }

    private static string Digest(GovernedHumanReviewRequest request, HumanReviewPolicyDecision policy,
        GovernedTestsExecutionReceipt tests, GovernedSandboxExecutionReceipt sandbox,
        GovernedSecurityValidationReceipt security, AuthorizedCodeGenerationCandidateSnapshot candidate)
    {
        var canonical = new StringBuilder().Append(request.ReviewId.ToString("D")).Append('|')
            .Append(request.Identity.TenantId).Append('|').Append(request.Identity.SubjectId).Append('|')
            .Append(request.Decision).Append('|').Append(request.Rationale).Append('|')
            .Append(request.HumanAttestationReference).Append('|').Append(request.ExpectedVersion).Append('|')
            .Append(request.ExpectedCandidateSha256Digest.ToLowerInvariant()).Append('|')
            .Append(request.ExpectedSecurityReportSha256Digest.ToLowerInvariant()).Append('|')
            .Append(request.ExpectedSandboxResultSha256Digest.ToLowerInvariant()).Append('|')
            .Append(request.ExpectedTestsResultSha256Digest.ToLowerInvariant()).Append('|')
            .Append(tests.TestManifestSha256Digest.ToLowerInvariant()).Append('|')
            .AppendJoin(',', tests.TestResults.OrderBy(item => item.TestId, StringComparer.Ordinal)
                .Select(item => $"{item.TestId}:{item.Passed}:{item.Skipped}:{item.EvidenceReference}"))
            .Append('|').AppendJoin(',', sandbox.ProducedArtifactReferences.Order(StringComparer.Ordinal))
            .Append('|').Append(security.ValidationEvidenceReference).Append('|')
            .Append(candidate.Receipt.GenerationEvidenceReference).Append('|')
            .Append(policy.DecisionRequestId.ToString("D")).Append('|')
            .Append(policy.BundleSha256Digest.ToLowerInvariant()).Append('|')
            .AppendJoin(',', policy.ConflictingSubjectIds.Order(StringComparer.Ordinal)).Append('|')
            .Append(policy.DecidedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }

    private static void ValidateAttestation(HumanReviewAttestationRequest request, HumanReviewAttestationDecision value)
    {
        if (value.ReviewId != request.ReviewId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.Ordinal.Equals(value.ReviewerSubjectId, request.ReviewerSubjectId) || value.Decision != request.Decision ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ReviewPackageSha256Digest, request.ReviewPackageSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.HumanAttestationReference, request.HumanAttestationReference) ||
            !value.IsHumanIdentityVerified || !value.IsSignatureValid || !value.IsNonRepudiable ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.VerifiedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Human Review attestation is invalid or mismatched.");
    }

    private static void ValidatePersisted(HumanReviewAtomicRecord record, long expectedVersion, HumanReviewAtomicReceipt value)
    {
        if (value.ReviewId != record.ReviewId || value.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, record.TenantId) ||
            !StringComparer.Ordinal.Equals(value.ReviewerSubjectId, record.ReviewerSubjectId) || value.Decision != record.Decision ||
            !StringComparer.Ordinal.Equals(value.Rationale, record.Rationale) ||
            !StringComparer.Ordinal.Equals(value.HumanAttestationReference, record.HumanAttestationReference) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ReviewPackageSha256Digest, record.ReviewPackageSha256Digest) ||
            value.Version != expectedVersion + 1 || string.IsNullOrWhiteSpace(value.EvidenceReference) ||
            value.EvidenceReferences.IsDefaultOrEmpty ||
            !record.EvidenceReferences.ToImmutableHashSet(StringComparer.Ordinal).IsSubsetOf(
                value.EvidenceReferences.ToImmutableHashSet(StringComparer.Ordinal)) ||
            value.RecordedAt < record.ReviewedAt)
            throw new InvalidOperationException("Atomic Human Review repository returned a mismatched receipt.");
    }
}

internal static class GovernedTestsReceiptExtensions
{
    internal static bool IsRecordedEquivalent(this GovernedTestsExecutionReceipt receipt) =>
        !string.IsNullOrWhiteSpace(receipt.ResultSha256Digest) && !string.IsNullOrWhiteSpace(receipt.TestsEvidenceReference);
}
