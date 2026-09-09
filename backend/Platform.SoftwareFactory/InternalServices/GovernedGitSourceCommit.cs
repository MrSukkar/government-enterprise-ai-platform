using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.InternalService;

public enum GovernedGitChangeOperation { Create, Update, Delete }

public sealed record GovernedGitFileChange(
    string RelativePath, GovernedGitChangeOperation Operation,
    string? ExpectedPreviousSha256Digest, string? NewContentSha256Digest, string? NewContent)
{
    public GovernedGitFileChange Validate()
    {
        GovernedGeneratedPath.Validate(RelativePath);
        if (!Enum.IsDefined(Operation)) throw new InvalidOperationException("A valid Git change operation is required.");
        if (Operation == GovernedGitChangeOperation.Create && ExpectedPreviousSha256Digest is not null ||
            Operation != GovernedGitChangeOperation.Create && ExpectedPreviousSha256Digest is null)
            throw new InvalidOperationException("Git change previous-content precondition is invalid.");
        if (ExpectedPreviousSha256Digest is not null)
            GovernedAiPlanningRequest.ValidateDigest(ExpectedPreviousSha256Digest, "previous file content");
        if (Operation == GovernedGitChangeOperation.Delete)
        {
            if (NewContentSha256Digest is not null || NewContent is not null)
                throw new InvalidOperationException("A Git delete cannot include new content.");
        }
        else
        {
            GovernedAiPlanningRequest.ValidateDigest(NewContentSha256Digest!, "new file content");
            ArgumentNullException.ThrowIfNull(NewContent);
            var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(NewContent))).ToLowerInvariant();
            if (!StringComparer.OrdinalIgnoreCase.Equals(digest, NewContentSha256Digest))
                throw new InvalidOperationException("Git change content digest is invalid.");
        }
        return this;
    }
}

public sealed record GovernedGitChangeSet(
    Guid GenerationId, string CandidateSha256Digest,
    ImmutableArray<GovernedGitFileChange> Changes, string ChangeSetSha256Digest)
{
    public GovernedGitChangeSet Validate()
    {
        if (GenerationId == Guid.Empty) throw new InvalidOperationException("Generation identity is required.");
        GovernedAiPlanningRequest.ValidateDigest(CandidateSha256Digest, "candidate");
        GovernedAiPlanningRequest.ValidateDigest(ChangeSetSha256Digest, "Git change set");
        if (Changes.IsDefaultOrEmpty || Changes.Any(item => item is null) ||
            Changes.GroupBy(item => item.RelativePath, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new InvalidOperationException("A unique non-empty Git change set is required.");
        foreach (var change in Changes) change.Validate();
        if (!StringComparer.OrdinalIgnoreCase.Equals(ChangeSetSha256Digest, CalculateDigest(this)))
            throw new InvalidOperationException("Git change-set digest is invalid.");
        return this;
    }

    internal static string CalculateDigest(GovernedGitChangeSet value)
    {
        var canonical = new StringBuilder().Append(value.GenerationId.ToString("D")).Append('|')
            .Append(value.CandidateSha256Digest.ToLowerInvariant());
        foreach (var item in value.Changes.OrderBy(item => item.RelativePath, StringComparer.Ordinal))
            canonical.Append('|').Append(item.RelativePath).Append(':').Append(item.Operation).Append(':')
                .Append(item.ExpectedPreviousSha256Digest?.ToLowerInvariant()).Append(':')
                .Append(item.NewContentSha256Digest?.ToLowerInvariant());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }
}

public sealed record GovernedGitAppliedChangeProof(
    string RelativePath, GovernedGitChangeOperation Operation,
    string? ExpectedPreviousSha256Digest, string? NewContentSha256Digest);

public sealed record GovernedGitSourceCommitRequest(
    Guid OperationId, Guid ReviewId, Guid TestsExecutionId, Guid GenerationId, Guid DeliveryRunId,
    string ExpectedCandidateSha256Digest, string ExpectedReviewPackageSha256Digest,
    string ExpectedReviewEvidenceReference, string ExpectedTestsResultSha256Digest,
    string ExpectedChangeSetSha256Digest, ImmutableHashSet<string> AuthorizedRelativePaths,
    string RepositoryId, string ExpectedBaseCommitId, string ChangeBranch,
    string CommitMessage, string SigningPolicyReference,
    GovernedIdentity Identity, string Purpose, DataClassification MaximumClassification,
    string AuthorizationEvidenceReference, string Environment,
    IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedGitSourceCommitRequest Validate()
    {
        if (OperationId == Guid.Empty || ReviewId == Guid.Empty || TestsExecutionId == Guid.Empty ||
            GenerationId == Guid.Empty || DeliveryRunId == Guid.Empty)
            throw new InvalidOperationException("Git operation and prerequisite identities are required.");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedCandidateSha256Digest, "candidate");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedReviewPackageSha256Digest, "review package");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedTestsResultSha256Digest, "Tests result");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedChangeSetSha256Digest, "Git change set");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedReviewEvidenceReference);
        if (AuthorizedRelativePaths.IsEmpty || AuthorizedRelativePaths.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("OPA-scoped Git paths are required.");
        foreach (var path in AuthorizedRelativePaths) GovernedGeneratedPath.Validate(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(RepositoryId);
        ValidateObjectId(ExpectedBaseCommitId, "base commit");
        ValidateBranch(ChangeBranch);
        ArgumentException.ThrowIfNullOrWhiteSpace(CommitMessage);
        if (CommitMessage != CommitMessage.Trim() || CommitMessage.Any(char.IsControl))
            throw new InvalidOperationException("Commit message metadata is invalid.");
        ArgumentException.ThrowIfNullOrWhiteSpace(SigningPolicyReference);
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("developer.internal-service.git.commit"))
            throw new UnauthorizedAccessException("Governed Git commit permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Git source mutation.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Git policy environment or time is invalid.");
        return this;
    }

    internal static void ValidateObjectId(string value, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length is not (40 or 64) || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException($"The {label} must be an immutable Git object identifier.");
    }

    private static void ValidateBranch(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value != value.Trim() || value.Any(char.IsControl) || value.Contains("..", StringComparison.Ordinal) ||
            value.IndexOfAny(['~', '^', ':', '?', '*', '[', '\\']) >= 0 || value.StartsWith('/') ||
            value.EndsWith('/') || value.EndsWith('.') || value.EndsWith(".lock", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("main", StringComparison.OrdinalIgnoreCase) || value.Equals("master", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The requested Git change branch is invalid or protected.");
    }
}

public interface IAuthorizedHumanReviewReceiptReader
{
    Task<GovernedHumanReviewReceipt?> LoadAsync(Guid reviewId, string tenantId, CancellationToken cancellationToken);
}

public interface IGitHumanReviewReceiptReader
{
    Task<GovernedHumanReviewReceipt?> LoadAsync(Guid reviewId,string tenantId,string purpose,
        Guid testsExecutionId,Guid generationId,Guid deliveryRunId,string candidateSha256Digest,
        string reviewPackageSha256Digest,string reviewEvidenceReference,string testsResultSha256Digest,
        CancellationToken cancellationToken);
}

public interface IGitTestsReceiptReader
{
    Task<GovernedTestsExecutionReceipt?> LoadAsync(Guid executionId,string tenantId,string purpose,
        Guid generationId,Guid deliveryRunId,string candidateSha256Digest,string resultSha256Digest,
        CancellationToken cancellationToken);
}

public interface IGitCandidateReader
{
    Task<AuthorizedCodeGenerationCandidateSnapshot?> LoadAsync(Guid generationId,string tenantId,string purpose,
        Guid deliveryRunId,string candidateSha256Digest,CancellationToken cancellationToken);
}

public interface IGitDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId,string tenantId,string purpose,Guid reviewId,
        Guid testsExecutionId,Guid generationId,string candidateSha256Digest,
        string reviewPackageSha256Digest,CancellationToken cancellationToken);
}

public interface IGovernedGitChangeSetMaterializer
{
    Task<GovernedGitChangeSet> MaterializeAsync(
        Guid operationId, AiCandidateArtifact candidate, ImmutableHashSet<string> authorizedPaths,
        CancellationToken cancellationToken);
}

public sealed record GitPolicyInput(
    Guid DecisionRequestId, Guid OperationId, Guid ReviewId, Guid TestsExecutionId,
    Guid GenerationId, Guid DeliveryRunId, string TenantId, string SubjectId, string Purpose,
    string Environment, DataClassification MaximumClassification, string CandidateSha256Digest,
    string ReviewPackageSha256Digest, string ReviewEvidenceReference, string TestsResultSha256Digest,
    string ChangeSetSha256Digest, ImmutableHashSet<string> AuthorizedRelativePaths,
    string RepositoryId, string ExpectedBaseCommitId, string ChangeBranch,
    string CommitMessage, string SigningPolicyReference, IntentPolicyBundleReference PolicyBundle,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset EvaluatedAt);

public sealed record GitPolicyDecision(
    Guid DecisionRequestId, Guid OperationId, Guid ReviewId, Guid TestsExecutionId,
    Guid GenerationId, Guid DeliveryRunId, string TenantId, string Environment,
    string CandidateSha256Digest, string ReviewPackageSha256Digest, string TestsResultSha256Digest,
    string ChangeSetSha256Digest, ImmutableHashSet<string> AllowedRelativePaths,
    string RepositoryId, string ExpectedBaseCommitId, string ChangeBranch,
    string CommitMessage, string SigningPolicyReference, bool ProtectedBranch,
    bool ForceUpdateAllowed, bool CiCdTriggerAllowed, string BundleId, string BundleVersion,
    string BundleSha256Digest, bool PolicySignatureValid, string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome, DataClassification MaximumClassification,
    ImmutableHashSet<string> RequiredRoles, string OutputKind,
    ImmutableArray<string> Reasons, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IGitPolicyGate
{
    Task<GitPolicyDecision> EvaluateAsync(GitPolicyInput input, CancellationToken cancellationToken);
}

public sealed record GitChangePolicyRequest(
    Guid OperationId, string TenantId, string RepositoryId, string ChangeBranch,
    string ExpectedBaseCommitId, string ReviewPackageSha256Digest,
    GovernedGitChangeSet ChangeSet, ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record GitChangePolicyDecision(
    Guid OperationId, string TenantId, string RepositoryId, string ChangeSetSha256Digest,
    bool IsAllowed, bool SecretsDetected, bool ProhibitedPathDetected, bool UnauthorizedDeleteDetected,
    bool BinarySubstitutionDetected, bool DependencyDriftDetected, bool PolicyTamperingDetected,
    bool UnrelatedContentDetected, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IGitChangePolicyValidator
{
    Task<GitChangePolicyDecision> ValidateAsync(GitChangePolicyRequest request, CancellationToken cancellationToken);
}

public sealed record InstitutionalGitCommitRequest(
    Guid OperationId, string TenantId, string SubjectId, string RepositoryId,
    string ExpectedBaseCommitId, string ChangeBranch, string CommitMessage,
    string SigningPolicyReference, GovernedGitChangeSet ChangeSet,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record InstitutionalGitCommitResult(
    Guid OperationId, string TenantId, string RepositoryId, string BaseCommitId,
    string ChangeBranch, string CommitId, string TreeSha256Digest,
    ImmutableArray<GovernedGitFileChange> AppliedChanges, bool CleanBaseVerified,
    bool CommitSigned, string SignatureEvidenceReference, bool SourceMutationOccurred,
    bool ProtectedBranchMutated, bool ForceUpdateOccurred, bool HistoryRewritten,
    bool PullRequestCreated, bool CiCdTriggered, bool ArtifactPublished,
    bool ProductionEffectOccurred, ImmutableArray<string> EvidenceReferences, DateTimeOffset CommittedAt);

public interface IInstitutionalGitGateway
{
    Task<InstitutionalGitCommitResult> CommitAsync(InstitutionalGitCommitRequest request, CancellationToken cancellationToken);
}

public sealed record GitResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid OperationId, string TenantId, string SubjectId,
    GovernedIdentity Identity, ImmutableHashSet<string> RequiredRoles,
    string Purpose, string Environment, DataClassification MaximumClassification,
    string CandidateSha256Digest, string ReviewPackageSha256Digest,
    string ChangeSetSha256Digest, InstitutionalGitCommitResult Commit,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record GitResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid OperationId, string TenantId, string CommitId,
    bool IsAllowed, string Code, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IGitResultAuthorizer
{
    Task<GitResultAuthorizationDecision> AuthorizeAsync(GitResultAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record GitEvidenceRecord(
    Guid OperationId, Guid ReviewId, Guid TestsExecutionId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, Guid PolicyDecisionRequestId, string ReviewPackageSha256Digest,
    GovernedGitChangeSet ChangeSet, InstitutionalGitCommitResult Commit,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset AuthorizedAt);

public sealed record GitEvidenceReceipt(
    Guid OperationId, Guid DeliveryRunId, string TenantId, string CommitId,
    string ChangeSetSha256Digest, string EvidenceReference, DateTimeOffset RecordedAt);

public interface IGitEvidenceRecorder
{
    Task<GitEvidenceReceipt> RecordAsync(GitEvidenceRecord record, CancellationToken cancellationToken);
}

public sealed class GitDependencyUnavailableException(string message) : Exception(message);

public sealed record GovernedGitSourceCommitReceipt(
    Guid OperationId, Guid ReviewId, Guid TestsExecutionId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, GovernedIntentPolicyOutcome PolicyOutcome, bool IsCommitted,
    bool SourceMutationOccurred, bool ProductionEffectOccurred, bool CiCdTriggered, bool CanAdvance,
    string CandidateSha256Digest, string ReviewPackageSha256Digest, string ChangeSetSha256Digest,
    string RepositoryId, string ChangeBranch, string? BaseCommitId, string? CommitId,
    string? TreeSha256Digest, bool CommitSigned, ImmutableArray<GovernedGitAppliedChangeProof> AppliedChanges,
    string? GitEvidenceReference, ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedGitSourceCommitEngine
{
    public async Task<GovernedGitSourceCommitReceipt> CommitAsync(
        GovernedGitSourceCommitRequest request, IGitPolicyGate policyGate,
        IGitHumanReviewReceiptReader reviewReader, IGitTestsReceiptReader testsReader,
        IGitCandidateReader candidateReader, IGitDeliveryRunReader runReader,
        IGovernedGitChangeSetMaterializer materializer, IGitChangePolicyValidator changeValidator,
        IInstitutionalGitGateway gitGateway, IGitResultAuthorizer resultAuthorizer,
        IGitEvidenceRecorder evidenceRecorder, CancellationToken cancellationToken)
    {
        request.Validate();
        var initialEvidence = ImmutableArray.Create(request.ExpectedReviewEvidenceReference, request.AuthorizationEvidenceReference);
        var input = new GitPolicyInput(
            Guid.NewGuid(), request.OperationId, request.ReviewId, request.TestsExecutionId,
            request.GenerationId, request.DeliveryRunId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Purpose, request.Environment, request.MaximumClassification,
            request.ExpectedCandidateSha256Digest, request.ExpectedReviewPackageSha256Digest,
            request.ExpectedReviewEvidenceReference, request.ExpectedTestsResultSha256Digest,
            request.ExpectedChangeSetSha256Digest, request.AuthorizedRelativePaths,
            request.RepositoryId, request.ExpectedBaseCommitId, request.ChangeBranch,
            request.CommitMessage, request.SigningPolicyReference, request.PolicyBundle,
            initialEvidence, request.RequestedAt);
        var policy = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidatePolicy(input, request.Identity, policy);
        var policyEvidence = initialEvidence.Append(policy.PolicyVerificationEvidenceReference)
            .Concat(policy.EvidenceReferences).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        if (policy.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedGitSourceCommitReceipt(
                request.OperationId, request.ReviewId, request.TestsExecutionId, request.GenerationId,
                request.DeliveryRunId, request.Identity.TenantId, policy.Outcome,
                false, false, false, false, false, request.ExpectedCandidateSha256Digest,
                request.ExpectedReviewPackageSha256Digest, request.ExpectedChangeSetSha256Digest,
                request.RepositoryId, request.ChangeBranch, null, null, null, false, [], null,
                policyEvidence, "Policy denial requires a new governed Git request", policy.DecidedAt);

        var review = await reviewReader.LoadAsync(request.ReviewId,request.Identity.TenantId,request.Purpose,
            request.TestsExecutionId,request.GenerationId,request.DeliveryRunId,request.ExpectedCandidateSha256Digest,
            request.ExpectedReviewPackageSha256Digest,request.ExpectedReviewEvidenceReference,
            request.ExpectedTestsResultSha256Digest,cancellationToken)
            ?? throw new KeyNotFoundException("Approving Human Review receipt was not found.");
        ValidateReview(request, review);
        var tests = await testsReader.LoadAsync(request.TestsExecutionId,request.Identity.TenantId,request.Purpose,
            request.GenerationId,request.DeliveryRunId,request.ExpectedCandidateSha256Digest,
            request.ExpectedTestsResultSha256Digest,cancellationToken)
            ?? throw new KeyNotFoundException("Governed Tests receipt was not found.");
        ValidateTests(request, tests);
        var candidate = await candidateReader.LoadAsync(request.GenerationId,request.Identity.TenantId,request.Purpose,
            request.DeliveryRunId,request.ExpectedCandidateSha256Digest,cancellationToken)
            ?? throw new KeyNotFoundException("Governed Code Generation candidate was not found.");
        ValidateCandidate(request, candidate);
        var run = await runReader.LoadAsync(request.DeliveryRunId,request.Identity.TenantId,request.Purpose,
            request.ReviewId,request.TestsExecutionId,request.GenerationId,request.ExpectedCandidateSha256Digest,
            request.ExpectedReviewPackageSha256Digest,cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);

        var changeSet = await materializer.MaterializeAsync(
            request.OperationId, candidate.Candidate, request.AuthorizedRelativePaths, cancellationToken);
        ValidateChangeSet(request, candidate, changeSet);
        var prerequisiteEvidence = policyEvidence.Concat(review.EvidenceReferences).Append(review.ReviewEvidenceReference!)
            .Concat(tests.EvidenceReferences).Append(tests.TestsEvidenceReference!)
            .Concat(candidate.Receipt.EvidenceReferences).Append(candidate.Receipt.GenerationEvidenceReference!)
            .Append(run.History[^1].EvidenceReference).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        var changePolicyRequest = new GitChangePolicyRequest(
            request.OperationId, request.Identity.TenantId, request.RepositoryId, request.ChangeBranch,
            request.ExpectedBaseCommitId, request.ExpectedReviewPackageSha256Digest,
            changeSet, prerequisiteEvidence, policy.DecidedAt);
        var changePolicy = await changeValidator.ValidateAsync(changePolicyRequest, cancellationToken);
        ValidateChangePolicy(changePolicyRequest, changePolicy);
        var mutationEvidence = prerequisiteEvidence.Concat(changePolicy.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var commitRequest = new InstitutionalGitCommitRequest(
            request.OperationId, request.Identity.TenantId, request.Identity.SubjectId,
            request.RepositoryId, request.ExpectedBaseCommitId, request.ChangeBranch,
            request.CommitMessage, request.SigningPolicyReference, changeSet,
            mutationEvidence, changePolicy.DecidedAt);
        var commit = await gitGateway.CommitAsync(commitRequest, cancellationToken);
        ValidateCommit(commitRequest, commit);
        var commitEvidence = mutationEvidence.Append(commit.SignatureEvidenceReference)
            .Concat(commit.EvidenceReferences).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        var authorizationRequest = new GitResultAuthorizationRequest(
            Guid.NewGuid(), request.OperationId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Identity, policy.RequiredRoles,
            request.Purpose, request.Environment, request.MaximumClassification,
            request.ExpectedCandidateSha256Digest, request.ExpectedReviewPackageSha256Digest,
            request.ExpectedChangeSetSha256Digest, commit, commitEvidence, commit.CommittedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
        ValidateAuthorization(authorizationRequest, authorization);
        var allEvidence = commitEvidence.Concat(authorization.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new GitEvidenceRecord(
            request.OperationId, request.ReviewId, request.TestsExecutionId, request.GenerationId,
            request.DeliveryRunId, request.Identity.TenantId, policy.DecisionRequestId,
            request.ExpectedReviewPackageSha256Digest, changeSet, commit, allEvidence, authorization.DecidedAt);
        var evidence = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidence(record, evidence);
        return new GovernedGitSourceCommitReceipt(
            request.OperationId, request.ReviewId, request.TestsExecutionId, request.GenerationId,
            request.DeliveryRunId, request.Identity.TenantId, policy.Outcome,
            true, true, false, false, false, request.ExpectedCandidateSha256Digest,
            request.ExpectedReviewPackageSha256Digest, request.ExpectedChangeSetSha256Digest,
            request.RepositoryId, request.ChangeBranch, commit.BaseCommitId, commit.CommitId,
            commit.TreeSha256Digest, true,
            commit.AppliedChanges.Select(item => new GovernedGitAppliedChangeProof(
                item.RelativePath, item.Operation, item.ExpectedPreviousSha256Digest,
                item.NewContentSha256Digest)).OrderBy(item => item.RelativePath, StringComparer.Ordinal).ToImmutableArray(),
            evidence.EvidenceReference,
            allEvidence.Append(evidence.EvidenceReference).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved CI/CD", evidence.RecordedAt);
    }

    private static void ValidatePolicy(GitPolicyInput input, GovernedIdentity identity, GitPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.OperationId != input.OperationId ||
            value.ReviewId != input.ReviewId || value.TestsExecutionId != input.TestsExecutionId ||
            value.GenerationId != input.GenerationId || value.DeliveryRunId != input.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, input.TenantId) || !StringComparer.Ordinal.Equals(value.Environment, input.Environment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, input.CandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ReviewPackageSha256Digest, input.ReviewPackageSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.TestsResultSha256Digest, input.TestsResultSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ChangeSetSha256Digest, input.ChangeSetSha256Digest) ||
            !value.AllowedRelativePaths.SetEquals(input.AuthorizedRelativePaths) ||
            !StringComparer.Ordinal.Equals(value.RepositoryId, input.RepositoryId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ExpectedBaseCommitId, input.ExpectedBaseCommitId) ||
            !StringComparer.Ordinal.Equals(value.ChangeBranch, input.ChangeBranch) ||
            !StringComparer.Ordinal.Equals(value.CommitMessage, input.CommitMessage) ||
            !StringComparer.Ordinal.Equals(value.SigningPolicyReference, input.SigningPolicyReference) ||
            !StringComparer.Ordinal.Equals(value.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(value.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Git decision.");
        if (!value.PolicySignatureValid || value.ProtectedBranch || value.ForceUpdateAllowed || value.CiCdTriggerAllowed ||
            value.RequiredRoles.IsEmpty || value.OutputKind != "git-commit-result" ||
            string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) || value.MaximumClassification > input.MaximumClassification ||
            value.MaximumClassification > identity.Clearance || value.Reasons.IsDefaultOrEmpty ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("Git OPA decision is invalid.");
    }

    private static void ValidateReview(GovernedGitSourceCommitRequest request, GovernedHumanReviewReceipt value)
    {
        if (value.ReviewId != request.ReviewId || value.TestsExecutionId != request.TestsExecutionId ||
            value.GenerationId != request.GenerationId || value.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.TestsResultSha256Digest, request.ExpectedTestsResultSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ReviewPackageSha256Digest, request.ExpectedReviewPackageSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.ReviewEvidenceReference, request.ExpectedReviewEvidenceReference) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsRecorded || !value.IsApproved ||
            value.Decision != HumanReviewDecision.Approve || value.ProductionEffectOccurred || value.CanAdvance ||
            string.IsNullOrWhiteSpace(value.Rationale) || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Approving Human Review prerequisite is invalid or mismatched.");
    }

    private static void ValidateTests(GovernedGitSourceCommitRequest request, GovernedTestsExecutionReceipt value)
    {
        if (value.ExecutionId != request.TestsExecutionId || value.GenerationId != request.GenerationId ||
            value.DeliveryRunId != request.DeliveryRunId || !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ResultSha256Digest, request.ExpectedTestsResultSha256Digest) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted ||
            value.ProductionEffectOccurred || value.CanAdvance || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Tests prerequisite is invalid or mismatched.");
    }

    private static void ValidateCandidate(GovernedGitSourceCommitRequest request, AuthorizedCodeGenerationCandidateSnapshot value)
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

    private static void ValidateRun(GovernedGitSourceCommitRequest request, SoftwareDeliveryRun run)
    {
        run.Validate();
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.HumanReview).ToArray();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.HumanReview || run.History.Length != expected.Length ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) || index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run is not stopped at a valid HumanReview boundary.");
    }

    private static void ValidateChangeSet(GovernedGitSourceCommitRequest request,
        AuthorizedCodeGenerationCandidateSnapshot candidate, GovernedGitChangeSet value)
    {
        value.Validate();
        if (value.GenerationId != request.GenerationId ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ChangeSetSha256Digest, request.ExpectedChangeSetSha256Digest) ||
            !value.Changes.Select(item => item.RelativePath).ToImmutableHashSet(StringComparer.Ordinal).SetEquals(request.AuthorizedRelativePaths) ||
            !request.AuthorizedRelativePaths.SetEquals(candidate.Candidate.GeneratedFilePaths))
            throw new UnauthorizedAccessException("Materialized Git change set is partial, substituted, or mismatched.");
    }

    private static void ValidateChangePolicy(GitChangePolicyRequest request, GitChangePolicyDecision value)
    {
        if (value.OperationId != request.OperationId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.Ordinal.Equals(value.RepositoryId, request.RepositoryId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ChangeSetSha256Digest, request.ChangeSet.ChangeSetSha256Digest) ||
            !value.IsAllowed || value.SecretsDetected || value.ProhibitedPathDetected || value.UnauthorizedDeleteDetected ||
            value.BinarySubstitutionDetected || value.DependencyDriftDetected || value.PolicyTamperingDetected ||
            value.UnrelatedContentDetected || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Institutional Git change policy denied or returned a mismatched decision.");
    }

    private static void ValidateCommit(InstitutionalGitCommitRequest request, InstitutionalGitCommitResult value)
    {
        GovernedGitSourceCommitRequest.ValidateObjectId(value.BaseCommitId, "returned base commit");
        GovernedGitSourceCommitRequest.ValidateObjectId(value.CommitId, "created commit");
        GovernedAiPlanningRequest.ValidateDigest(value.TreeSha256Digest, "Git tree");
        if (value.OperationId != request.OperationId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.Ordinal.Equals(value.RepositoryId, request.RepositoryId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.BaseCommitId, request.ExpectedBaseCommitId) ||
            !StringComparer.Ordinal.Equals(value.ChangeBranch, request.ChangeBranch) || value.CommitId.Equals(value.BaseCommitId, StringComparison.OrdinalIgnoreCase) ||
            !value.AppliedChanges.OrderBy(item => item.RelativePath, StringComparer.Ordinal)
                .SequenceEqual(request.ChangeSet.Changes.OrderBy(item => item.RelativePath, StringComparer.Ordinal)) ||
            !value.CleanBaseVerified || !value.CommitSigned ||
            string.IsNullOrWhiteSpace(value.SignatureEvidenceReference) || !value.SourceMutationOccurred ||
            value.ProtectedBranchMutated || value.ForceUpdateOccurred || value.HistoryRewritten || value.PullRequestCreated ||
            value.CiCdTriggered || value.ArtifactPublished || value.ProductionEffectOccurred ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.CommittedAt < request.RequestedAt)
            throw new InvalidOperationException("Institutional Git gateway returned an invalid or unsafe commit proof.");
    }

    private static void ValidateAuthorization(GitResultAuthorizationRequest request, GitResultAuthorizationDecision value)
    {
        if (value.AuthorizationRequestId != request.AuthorizationRequestId || value.OperationId != request.OperationId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CommitId, request.Commit.CommitId) || !value.IsAllowed ||
            string.IsNullOrWhiteSpace(value.Code) || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Git result authorization denied or mismatched.");
    }

    private static void ValidateEvidence(GitEvidenceRecord record, GitEvidenceReceipt value)
    {
        if (value.OperationId != record.OperationId || value.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CommitId, record.Commit.CommitId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ChangeSetSha256Digest, record.ChangeSet.ChangeSetSha256Digest) ||
            string.IsNullOrWhiteSpace(value.EvidenceReference) || value.RecordedAt < record.AuthorizedAt)
            throw new InvalidOperationException("Git evidence receipt is invalid.");
    }
}
