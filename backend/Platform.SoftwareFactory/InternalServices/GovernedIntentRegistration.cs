using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.SoftwareFactory.InternalService;

public sealed record IntentPolicyBundleReference(
    string BundleId,
    string Version,
    string Sha256Digest,
    string SignatureReference,
    string Environment,
    DateTimeOffset ActivatedAt)
{
    public IntentPolicyBundleReference Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(BundleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        if (Sha256Digest.Length != 64 || Sha256Digest.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Intent policy bundle requires a SHA-256 digest.");
        if (ActivatedAt == default)
            throw new InvalidOperationException("Intent policy activation time is required.");
        return this;
    }
}

public sealed record GovernedIntentRegistrationRequest(
    Guid RegistrationId,
    GovernedIntentSubmission Submission,
    string SubjectId,
    string SubjectTenantId,
    DataClassification SubjectClearance,
    ImmutableHashSet<string> Permissions,
    string AuthorizationEvidenceReference,
    string Environment,
    IntentPolicyBundleReference PolicyBundle,
    long ExpectedVersion,
    DateTimeOffset RequestedAt)
{
    public GovernedIntentRegistrationRequest Validate()
    {
        if (RegistrationId == Guid.Empty)
            throw new InvalidOperationException("Intent registration identity is required.");
        ArgumentNullException.ThrowIfNull(Submission);
        Submission.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectTenantId);
        ArgumentNullException.ThrowIfNull(Permissions);
        if (!Permissions.Contains("developer.internal-service.intent.register"))
            throw new UnauthorizedAccessException("The developer.internal-service.intent.register permission is required.");
        if (!StringComparer.Ordinal.Equals(SubjectTenantId, Submission.TenantId))
            throw new UnauthorizedAccessException("Intent registration is outside the authenticated tenant scope.");
        if (SubjectClearance < Submission.Classification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for intent registration.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        if (!StringComparer.Ordinal.Equals(
                AuthorizationEvidenceReference,
                Submission.AuthorizationEvidenceReference))
            throw new UnauthorizedAccessException("Intent authorization evidence does not match the authenticated context.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentNullException.ThrowIfNull(PolicyBundle);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment))
            throw new InvalidOperationException("Intent policy bundle environment does not match registration environment.");
        if (ExpectedVersion < -1)
            throw new InvalidOperationException("Expected registration version must be -1 for create or a non-negative version.");
        if (RequestedAt == default || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Intent registration time is invalid for the active policy bundle.");
        return this;
    }
}

public sealed record GovernedIntentPolicyInput(
    Guid DecisionRequestId,
    Guid RegistrationId,
    string TenantId,
    string SubjectId,
    string Purpose,
    DataClassification Classification,
    string Environment,
    string Action,
    string ResourceId,
    string IntentSha256Digest,
    IntentPolicyBundleReference PolicyBundle,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset EvaluatedAt);

public enum GovernedIntentPolicyOutcome { Deny = 0, Permit = 1 }

public sealed record GovernedIntentPolicyDecision(
    Guid DecisionRequestId,
    Guid RegistrationId,
    string TenantId,
    string BundleId,
    string BundleVersion,
    string BundleSha256Digest,
    string Environment,
    bool PolicySignatureValid,
    string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome,
    ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IGovernedIntentPolicyGate
{
    Task<GovernedIntentPolicyDecision> EvaluateAsync(
        GovernedIntentPolicyInput input,
        CancellationToken cancellationToken);
}

public sealed record RegisteredGovernedIntent(
    Guid RegistrationId,
    Guid SubmissionId,
    string TenantId,
    string SubjectId,
    DataClassification Classification,
    string Purpose,
    string ServiceName,
    string Mission,
    string PrimaryUsers,
    string IntentSha256Digest,
    Guid PolicyDecisionRequestId,
    string PolicyBundleId,
    string PolicyBundleVersion,
    string PolicyBundleSha256Digest,
    string IdempotencyKey,
    long Version,
    ImmutableArray<string> EvidenceReferences,
    string RegistrationEvidenceReference,
    DateTimeOffset RegisteredAt);

public enum GovernedIntentRegistrationDisposition { Created = 0, Unchanged = 1 }

public sealed record GovernedIntentRegistrationResult(
    GovernedIntentRegistrationDisposition Disposition,
    RegisteredGovernedIntent Intent);

public interface IGovernedIntentRegistrationRepository
{
    Task<GovernedIntentRegistrationResult> RegisterAtomicallyAsync(
        RegisteredGovernedIntent candidate,
        long expectedVersion,
        CancellationToken cancellationToken);
}

public sealed class GovernedIntentConcurrencyException(string message) : Exception(message);

public sealed record GovernedIntentRegistrationReceipt(
    Guid RegistrationId,
    Guid SubmissionId,
    string TenantId,
    string IntentSha256Digest,
    GovernedIntentPolicyOutcome PolicyOutcome,
    bool IsPersisted,
    bool CanAdvance,
    GovernedIntentRegistrationDisposition? Disposition,
    long? Version,
    string? RegistrationEvidenceReference,
    ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate,
    DateTimeOffset CompletedAt);

public sealed class GovernedIntentRegistrationEngine(GovernedIntentSubmissionValidator validator)
{
    private readonly GovernedIntentSubmissionValidator _validator = validator;

    public async Task<GovernedIntentRegistrationReceipt> RegisterAsync(
        GovernedIntentRegistrationRequest request,
        IGovernedIntentPolicyGate policyGate,
        IGovernedIntentRegistrationRepository repository,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(policyGate);
        ArgumentNullException.ThrowIfNull(repository);
        request.Validate();

        var validation = _validator.Validate(request.Submission, request.SubjectId, request.RequestedAt);
        var decisionRequestId = Guid.NewGuid();
        var policyInput = new GovernedIntentPolicyInput(
            decisionRequestId,
            request.RegistrationId,
            request.Submission.TenantId,
            request.SubjectId,
            request.Submission.Purpose,
            request.Submission.Classification,
            request.Environment,
            "internal-service.intent.register",
            request.Submission.SubmissionId.ToString("D"),
            validation.IntentDigest,
            request.PolicyBundle,
            validation.EvidenceReferences,
            request.RequestedAt);

        var decision = await policyGate.EvaluateAsync(policyInput, cancellationToken);
        ValidateDecision(policyInput, decision);

        var decisionEvidence = validation.EvidenceReferences
            .Add(decision.PolicyVerificationEvidenceReference)
            .AddRange(decision.EvidenceReferences)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (decision.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedIntentRegistrationReceipt(
                request.RegistrationId,
                request.Submission.SubmissionId,
                request.Submission.TenantId,
                validation.IntentDigest,
                decision.Outcome,
                IsPersisted: false,
                CanAdvance: false,
                Disposition: null,
                Version: null,
                RegistrationEvidenceReference: null,
                decisionEvidence,
                "Policy denial requires a new governed intent submission",
                decision.DecidedAt);

        var idempotencyKey = $"governed-intent:{request.RegistrationId:D}:{validation.IntentDigest}";
        var candidate = new RegisteredGovernedIntent(
            request.RegistrationId,
            request.Submission.SubmissionId,
            request.Submission.TenantId,
            request.SubjectId,
            request.Submission.Classification,
            request.Submission.Purpose,
            request.Submission.ServiceName,
            request.Submission.Mission,
            request.Submission.PrimaryUsers,
            validation.IntentDigest,
            decision.DecisionRequestId,
            decision.BundleId,
            decision.BundleVersion,
            decision.BundleSha256Digest,
            idempotencyKey,
            Version: request.ExpectedVersion + 1,
            decisionEvidence,
            RegistrationEvidenceReference: "pending-atomic-registration",
            RegisteredAt: decision.DecidedAt);

        var persisted = await repository.RegisterAtomicallyAsync(
            candidate,
            request.ExpectedVersion,
            cancellationToken);
        var registered = ValidatePersisted(request, candidate, persisted);
        var completedEvidence = registered.EvidenceReferences
            .Append(registered.RegistrationEvidenceReference)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();

        return new GovernedIntentRegistrationReceipt(
            registered.RegistrationId,
            registered.SubmissionId,
            registered.TenantId,
            registered.IntentSha256Digest,
            decision.Outcome,
            IsPersisted: true,
            CanAdvance: false,
            persisted.Disposition,
            registered.Version,
            registered.RegistrationEvidenceReference,
            completedEvidence,
            "Authorized Enterprise Context discovery",
            registered.RegisteredAt);
    }

    private static void ValidateDecision(
        GovernedIntentPolicyInput input,
        GovernedIntentPolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.DecisionRequestId != input.DecisionRequestId ||
            decision.RegistrationId != input.RegistrationId ||
            !StringComparer.Ordinal.Equals(decision.TenantId, input.TenantId) ||
            !StringComparer.Ordinal.Equals(decision.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(decision.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.BundleSha256Digest, input.PolicyBundle.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(decision.Environment, input.Environment) ||
            !decision.PolicySignatureValid ||
            !Enum.IsDefined(decision.Outcome))
            throw new UnauthorizedAccessException("OPA returned a mismatched intent decision; registration denied fail closed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(decision.PolicyVerificationEvidenceReference);
        if (decision.Reasons.IsDefaultOrEmpty || decision.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("OPA intent decisions require reasons and evidence.");
        foreach (var value in decision.Reasons) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var value in decision.EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (decision.DecidedAt < input.EvaluatedAt)
            throw new InvalidOperationException("OPA intent decision predates evaluation.");
    }

    private static RegisteredGovernedIntent ValidatePersisted(
        GovernedIntentRegistrationRequest request,
        RegisteredGovernedIntent candidate,
        GovernedIntentRegistrationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(result.Intent);
        if (!Enum.IsDefined(result.Disposition))
            throw new InvalidOperationException("Intent repository returned an invalid disposition.");
        var persisted = result.Intent;
        if (persisted.RegistrationId != candidate.RegistrationId ||
            persisted.SubmissionId != candidate.SubmissionId ||
            !StringComparer.Ordinal.Equals(persisted.TenantId, candidate.TenantId) ||
            !StringComparer.Ordinal.Equals(persisted.SubjectId, candidate.SubjectId) ||
            persisted.Classification != candidate.Classification ||
            !StringComparer.Ordinal.Equals(persisted.Purpose, candidate.Purpose) ||
            !StringComparer.Ordinal.Equals(persisted.ServiceName, candidate.ServiceName) ||
            !StringComparer.Ordinal.Equals(persisted.Mission, candidate.Mission) ||
            !StringComparer.Ordinal.Equals(persisted.PrimaryUsers, candidate.PrimaryUsers) ||
            !StringComparer.OrdinalIgnoreCase.Equals(persisted.IntentSha256Digest, candidate.IntentSha256Digest) ||
            persisted.PolicyDecisionRequestId != candidate.PolicyDecisionRequestId ||
            !StringComparer.Ordinal.Equals(persisted.PolicyBundleId, candidate.PolicyBundleId) ||
            !StringComparer.Ordinal.Equals(persisted.PolicyBundleVersion, candidate.PolicyBundleVersion) ||
            !StringComparer.OrdinalIgnoreCase.Equals(persisted.PolicyBundleSha256Digest, candidate.PolicyBundleSha256Digest) ||
            !StringComparer.Ordinal.Equals(persisted.IdempotencyKey, candidate.IdempotencyKey) ||
            !candidate.EvidenceReferences.All(persisted.EvidenceReferences.Contains) ||
            string.IsNullOrWhiteSpace(persisted.RegistrationEvidenceReference) ||
            StringComparer.Ordinal.Equals(persisted.RegistrationEvidenceReference, "pending-atomic-registration") ||
            persisted.RegisteredAt < candidate.RegisteredAt)
            throw new InvalidOperationException("Intent repository changed governed registration state.");
        if (result.Disposition == GovernedIntentRegistrationDisposition.Created &&
            persisted.Version != request.ExpectedVersion + 1)
            throw new InvalidOperationException("Created intent registration version is invalid.");
        if (result.Disposition == GovernedIntentRegistrationDisposition.Unchanged &&
            persisted.Version < 0)
            throw new InvalidOperationException("Unchanged intent registration version is invalid.");
        return persisted;
    }
}
