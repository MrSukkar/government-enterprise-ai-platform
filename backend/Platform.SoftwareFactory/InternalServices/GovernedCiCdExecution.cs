using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedCiCdExecutionRequest(
    Guid ExecutionId, Guid GitOperationId, Guid DeliveryRunId,
    string RepositoryId, string ExpectedCommitId, string ExpectedTreeSha256Digest,
    string ExpectedChangeSetSha256Digest, string WorkflowDefinitionId,
    string WorkflowDefinitionVersion, string ExpectedWorkflowSha256Digest,
    string WorkflowSignatureReference, string PipelineProfile, string RunnerPoolId,
    ImmutableArray<string> RequiredStageIds, ImmutableHashSet<string> RequiredControlIds,
    GovernedIdentity Identity, string Purpose, DataClassification MaximumClassification,
    string AuthorizationEvidenceReference, string Environment,
    IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedCiCdExecutionRequest Validate()
    {
        if (ExecutionId == Guid.Empty || GitOperationId == Guid.Empty || DeliveryRunId == Guid.Empty)
            throw new InvalidOperationException("CI/CD and prerequisite identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(RepositoryId);
        GovernedGitSourceCommitRequest.ValidateObjectId(ExpectedCommitId, "CI/CD source commit");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedTreeSha256Digest, "Git tree");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedChangeSetSha256Digest, "Git change set");
        ArgumentException.ThrowIfNullOrWhiteSpace(WorkflowDefinitionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(WorkflowDefinitionVersion);
        GovernedAiPlanningRequest.ValidateDigest(ExpectedWorkflowSha256Digest, "CI/CD workflow");
        ArgumentException.ThrowIfNullOrWhiteSpace(WorkflowSignatureReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(PipelineProfile);
        ArgumentException.ThrowIfNullOrWhiteSpace(RunnerPoolId);
        if (RequiredStageIds.IsDefaultOrEmpty || RequiredStageIds.Any(string.IsNullOrWhiteSpace) ||
            RequiredStageIds.Distinct(StringComparer.Ordinal).Count() != RequiredStageIds.Length)
            throw new InvalidOperationException("Unique ordered CI/CD stages are required.");
        if (RequiredControlIds.IsEmpty || RequiredControlIds.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Unique CI/CD controls are required.");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("developer.internal-service.cicd.execute"))
            throw new UnauthorizedAccessException("Governed CI/CD execute permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for CI/CD execution.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("CI/CD policy environment or time is invalid.");
        return this;
    }
}

public interface IAuthorizedGitSourceCommitReceiptReader
{
    Task<GovernedGitSourceCommitReceipt?> LoadAsync(Guid operationId, string tenantId, CancellationToken cancellationToken);
}

public interface ICiCdDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken);
}

public sealed record GovernedCiCdWorkflowStage(
    string StageId, string ImmutableTaskReference, ImmutableArray<string> RequiredControlIds,
    bool UsesLockedDependencies, ImmutableArray<string> AuthorizedInstitutionalEndpoints);

public sealed record GovernedCiCdWorkflowDefinition(
    string WorkflowDefinitionId, string Version, string Sha256Digest,
    string SignatureEvidenceReference, bool SignatureValid, bool LeastPrivilege,
    bool ImmutableTaskReferences, ImmutableArray<GovernedCiCdWorkflowStage> Stages,
    ImmutableArray<string> EvidenceReferences);

public interface IGovernedCiCdWorkflowDefinitionReader
{
    Task<GovernedCiCdWorkflowDefinition?> LoadAsync(
        string workflowDefinitionId, string version, string tenantId, CancellationToken cancellationToken);
}

public sealed record CiCdPolicyInput(
    Guid DecisionRequestId, Guid ExecutionId, Guid GitOperationId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, string RepositoryId, string CommitId,
    string TreeSha256Digest, string ChangeSetSha256Digest, string WorkflowDefinitionId,
    string WorkflowDefinitionVersion, string WorkflowSha256Digest, string WorkflowSignatureReference,
    string PipelineProfile, string RunnerPoolId, ImmutableArray<string> RequiredStageIds,
    ImmutableHashSet<string> RequiredControlIds, IntentPolicyBundleReference PolicyBundle,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset EvaluatedAt);

public sealed record CiCdPolicyDecision(
    Guid DecisionRequestId, Guid ExecutionId, Guid GitOperationId, Guid DeliveryRunId,
    string TenantId, string Environment, string RepositoryId, string CommitId,
    string TreeSha256Digest, string ChangeSetSha256Digest, string WorkflowDefinitionId,
    string WorkflowDefinitionVersion, string WorkflowSha256Digest, string PipelineProfile,
    string RunnerPoolId, ImmutableArray<string> AllowedStageIds, ImmutableHashSet<string> AllowedControlIds,
    bool SourceMutationAllowed, bool ArtifactPublicationAllowed, bool DeploymentAllowed,
    string BundleId, string BundleVersion, string BundleSha256Digest, bool PolicySignatureValid,
    string PolicyVerificationEvidenceReference, GovernedIntentPolicyOutcome Outcome,
    DataClassification MaximumClassification, ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ICiCdPolicyGate
{
    Task<CiCdPolicyDecision> EvaluateAsync(CiCdPolicyInput input, CancellationToken cancellationToken);
}

public sealed record CiCdWorkflowValidationRequest(
    Guid ExecutionId, string TenantId, string RepositoryId, string CommitId,
    string PipelineProfile, string RunnerPoolId, GovernedCiCdWorkflowDefinition Workflow,
    ImmutableArray<string> RequiredStageIds, ImmutableHashSet<string> RequiredControlIds,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record CiCdWorkflowValidationDecision(
    Guid ExecutionId, string TenantId, string WorkflowSha256Digest, bool IsAllowed,
    bool MutableTaskReferenceDetected, bool DynamicSubstitutionDetected,
    bool UnapprovedCommandOrDownloadDetected, bool UnauthorizedRegistryDetected,
    bool SecretMaterialDetected, bool ProductionCredentialDetected,
    bool PrivilegedExecutionDetected, bool PolicyTamperingDetected,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ICiCdWorkflowValidator
{
    Task<CiCdWorkflowValidationDecision> ValidateAsync(
        CiCdWorkflowValidationRequest request, CancellationToken cancellationToken);
}

public sealed record GovernedCiCdStageResult(
    string StageId, bool Completed, bool Succeeded, bool Skipped, bool TimedOut,
    ImmutableArray<string> ControlIds, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CompletedAt);

public sealed record GovernedPipelineOutputManifest(
    string ManifestSha256Digest, string RepositoryId, string CommitId,
    string TreeSha256Digest, string WorkflowSha256Digest, string RunnerIdentity,
    string DependencyLockSha256Digest, ImmutableArray<string> OutputSha256Digests,
    string SbomReference, string ChecksumsReference, string ProvenanceReference,
    string BuildAttestationReference, string SignatureReference, bool IsReleasedArtifact,
    ImmutableArray<string> EvidenceReferences);

public sealed record InstitutionalCiCdExecutionRequest(
    Guid ExecutionId, string TenantId, string SubjectId, string RepositoryId,
    string CommitId, string TreeSha256Digest, string PipelineProfile, string RunnerPoolId,
    GovernedCiCdWorkflowDefinition Workflow, ImmutableArray<string> RequiredStageIds,
    ImmutableHashSet<string> RequiredControlIds, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RequestedAt);

public sealed record InstitutionalCiCdExecutionResult(
    Guid ExecutionId, string TenantId, string RepositoryId, string CommitId,
    string WorkflowSha256Digest, string RunnerPoolId, string RunnerIdentity,
    bool EphemeralIsolationVerified, bool NetworkDefaultDeny, bool ProductionCredentialsPresent,
    bool LockedDependencyRestore, ImmutableArray<GovernedCiCdStageResult> Stages,
    GovernedPipelineOutputManifest OutputManifest, bool CiCdTriggered,
    bool SourceMutationOccurred, bool PullRequestCreated, bool MergeOccurred, bool TagCreated,
    bool ArtifactPublished, bool RegistryMutated, bool DeploymentOccurred,
    bool ProductionEffectOccurred, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CompletedAt);

public interface IInstitutionalCiCdGateway
{
    Task<InstitutionalCiCdExecutionResult> ExecuteAsync(
        InstitutionalCiCdExecutionRequest request, CancellationToken cancellationToken);
}

public sealed record CiCdResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid ExecutionId, string TenantId, string SubjectId,
    string Purpose, string Environment, DataClassification MaximumClassification,
    InstitutionalCiCdExecutionResult Result, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RequestedAt);

public sealed record CiCdResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid ExecutionId, string TenantId,
    string ManifestSha256Digest, bool IsAllowed, string Code,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ICiCdResultAuthorizer
{
    Task<CiCdResultAuthorizationDecision> AuthorizeAsync(
        CiCdResultAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record CiCdEvidenceRecord(
    Guid ExecutionId, Guid GitOperationId, Guid DeliveryRunId, string TenantId,
    Guid PolicyDecisionRequestId, GovernedCiCdWorkflowDefinition Workflow,
    InstitutionalCiCdExecutionResult Result, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset AuthorizedAt);

public sealed record CiCdEvidenceReceipt(
    Guid ExecutionId, Guid DeliveryRunId, string TenantId, string ManifestSha256Digest,
    string EvidenceReference, DateTimeOffset RecordedAt);

public interface ICiCdEvidenceRecorder
{
    Task<CiCdEvidenceReceipt> RecordAsync(CiCdEvidenceRecord record, CancellationToken cancellationToken);
}

public sealed class CiCdDependencyUnavailableException(string message) : Exception(message);

public sealed record GovernedCiCdExecutionReceipt(
    Guid ExecutionId, Guid GitOperationId, Guid DeliveryRunId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, bool IsAccepted, bool CiCdTriggered,
    bool SourceMutationOccurred, bool ArtifactPublished, bool DeploymentOccurred,
    bool ProductionEffectOccurred, bool CanAdvance, string RepositoryId, string CommitId,
    string WorkflowSha256Digest, string? PipelineOutputManifestSha256Digest,
    ImmutableArray<GovernedCiCdStageResult> Stages, string? CiCdEvidenceReference,
    ImmutableArray<string> EvidenceReferences, string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedCiCdExecutionEngine
{
    public async Task<GovernedCiCdExecutionReceipt> ExecuteAsync(
        GovernedCiCdExecutionRequest request, ICiCdPolicyGate policyGate,
        IAuthorizedGitSourceCommitReceiptReader gitReader, ICiCdDeliveryRunReader runReader,
        IGovernedCiCdWorkflowDefinitionReader workflowReader, ICiCdWorkflowValidator workflowValidator,
        IInstitutionalCiCdGateway gateway, ICiCdResultAuthorizer resultAuthorizer,
        ICiCdEvidenceRecorder evidenceRecorder, CancellationToken cancellationToken)
    {
        request.Validate();
        var input = new CiCdPolicyInput(
            Guid.NewGuid(), request.ExecutionId, request.GitOperationId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, request.RepositoryId, request.ExpectedCommitId,
            request.ExpectedTreeSha256Digest, request.ExpectedChangeSetSha256Digest,
            request.WorkflowDefinitionId, request.WorkflowDefinitionVersion,
            request.ExpectedWorkflowSha256Digest, request.WorkflowSignatureReference,
            request.PipelineProfile, request.RunnerPoolId, request.RequiredStageIds,
            request.RequiredControlIds, request.PolicyBundle,
            [request.AuthorizationEvidenceReference, request.WorkflowSignatureReference], request.RequestedAt);
        var policy = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidatePolicy(input, request.Identity, policy);
        var policyEvidence = input.EvidenceReferences.Append(policy.PolicyVerificationEvidenceReference)
            .Concat(policy.EvidenceReferences).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        if (policy.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedCiCdExecutionReceipt(
                request.ExecutionId, request.GitOperationId, request.DeliveryRunId, request.Identity.TenantId,
                policy.Outcome, false, false, false, false, false, false, false,
                request.RepositoryId, request.ExpectedCommitId, request.ExpectedWorkflowSha256Digest,
                null, [], null, policyEvidence, "Policy denial requires a new governed CI/CD request", policy.DecidedAt);

        var git = await gitReader.LoadAsync(request.GitOperationId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Git receipt was not found.");
        ValidateGit(request, git);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);
        var workflow = await workflowReader.LoadAsync(
            request.WorkflowDefinitionId, request.WorkflowDefinitionVersion,
            request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed CI/CD workflow definition was not found.");
        ValidateWorkflow(request, workflow);

        var prerequisiteEvidence = policyEvidence.Concat(git.EvidenceReferences)
            .Append(git.GitEvidenceReference!).Append(run.History[^1].EvidenceReference)
            .Concat(workflow.EvidenceReferences).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var validationRequest = new CiCdWorkflowValidationRequest(
            request.ExecutionId, request.Identity.TenantId, request.RepositoryId,
            request.ExpectedCommitId, request.PipelineProfile, request.RunnerPoolId,
            workflow, request.RequiredStageIds, request.RequiredControlIds,
            prerequisiteEvidence, policy.DecidedAt);
        var validation = await workflowValidator.ValidateAsync(validationRequest, cancellationToken);
        ValidateWorkflowDecision(validationRequest, validation);
        var executionEvidence = prerequisiteEvidence.Concat(validation.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var executionRequest = new InstitutionalCiCdExecutionRequest(
            request.ExecutionId, request.Identity.TenantId, request.Identity.SubjectId,
            request.RepositoryId, request.ExpectedCommitId, request.ExpectedTreeSha256Digest,
            request.PipelineProfile, request.RunnerPoolId, workflow, request.RequiredStageIds,
            request.RequiredControlIds, executionEvidence, validation.DecidedAt);
        var result = await gateway.ExecuteAsync(executionRequest, cancellationToken);
        ValidateResult(executionRequest, result);
        var resultEvidence = executionEvidence.Concat(result.EvidenceReferences)
            .Concat(result.OutputManifest.EvidenceReferences).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        var authorizationRequest = new CiCdResultAuthorizationRequest(
            Guid.NewGuid(), request.ExecutionId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Purpose, request.Environment, request.MaximumClassification,
            result, resultEvidence, result.CompletedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
        ValidateAuthorization(authorizationRequest, authorization);
        var allEvidence = resultEvidence.Concat(authorization.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new CiCdEvidenceRecord(
            request.ExecutionId, request.GitOperationId, request.DeliveryRunId,
            request.Identity.TenantId, policy.DecisionRequestId, workflow, result,
            allEvidence, authorization.DecidedAt);
        var evidence = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidence(record, evidence);
        return new GovernedCiCdExecutionReceipt(
            request.ExecutionId, request.GitOperationId, request.DeliveryRunId, request.Identity.TenantId,
            policy.Outcome, true, true, false, false, false, false, false,
            request.RepositoryId, request.ExpectedCommitId, request.ExpectedWorkflowSha256Digest,
            result.OutputManifest.ManifestSha256Digest, result.Stages, evidence.EvidenceReference,
            allEvidence.Append(evidence.EvidenceReference).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved Artifact", evidence.RecordedAt);
    }

    private static void ValidatePolicy(CiCdPolicyInput input, GovernedIdentity identity, CiCdPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.ExecutionId != input.ExecutionId ||
            value.GitOperationId != input.GitOperationId || value.DeliveryRunId != input.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, input.TenantId) ||
            !StringComparer.Ordinal.Equals(value.Environment, input.Environment) ||
            !StringComparer.Ordinal.Equals(value.RepositoryId, input.RepositoryId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CommitId, input.CommitId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.TreeSha256Digest, input.TreeSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ChangeSetSha256Digest, input.ChangeSetSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.WorkflowDefinitionId, input.WorkflowDefinitionId) ||
            !StringComparer.Ordinal.Equals(value.WorkflowDefinitionVersion, input.WorkflowDefinitionVersion) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.WorkflowSha256Digest, input.WorkflowSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.PipelineProfile, input.PipelineProfile) ||
            !StringComparer.Ordinal.Equals(value.RunnerPoolId, input.RunnerPoolId) ||
            !value.AllowedStageIds.SequenceEqual(input.RequiredStageIds) ||
            !value.AllowedControlIds.SetEquals(input.RequiredControlIds) ||
            !StringComparer.Ordinal.Equals(value.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(value.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched CI/CD decision.");
        if (!value.PolicySignatureValid || value.SourceMutationAllowed || value.ArtifactPublicationAllowed ||
            value.DeploymentAllowed || string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) ||
            value.MaximumClassification > input.MaximumClassification || value.MaximumClassification > identity.Clearance ||
            value.Reasons.IsDefaultOrEmpty || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("CI/CD OPA decision is invalid.");
    }

    private static void ValidateGit(GovernedCiCdExecutionRequest request, GovernedGitSourceCommitReceipt value)
    {
        if (value.OperationId != request.GitOperationId || value.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.Ordinal.Equals(value.RepositoryId, request.RepositoryId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CommitId, request.ExpectedCommitId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.TreeSha256Digest, request.ExpectedTreeSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ChangeSetSha256Digest, request.ExpectedChangeSetSha256Digest) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsCommitted || !value.CommitSigned ||
            !value.SourceMutationOccurred || value.ProductionEffectOccurred || value.CiCdTriggered || value.CanAdvance ||
            string.IsNullOrWhiteSpace(value.GitEvidenceReference) || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Governed Git prerequisite is invalid or mismatched.");
    }

    private static void ValidateRun(GovernedCiCdExecutionRequest request, SoftwareDeliveryRun run)
    {
        run.Validate();
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.Git).ToArray();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.Git || run.History.Length != expected.Length ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) ||
                index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run is not stopped at a valid Git boundary.");
    }

    private static void ValidateWorkflow(GovernedCiCdExecutionRequest request, GovernedCiCdWorkflowDefinition value)
    {
        if (!StringComparer.Ordinal.Equals(value.WorkflowDefinitionId, request.WorkflowDefinitionId) ||
            !StringComparer.Ordinal.Equals(value.Version, request.WorkflowDefinitionVersion) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.Sha256Digest, request.ExpectedWorkflowSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.SignatureEvidenceReference, request.WorkflowSignatureReference) ||
            !value.SignatureValid || !value.LeastPrivilege || !value.ImmutableTaskReferences ||
            !value.Stages.Select(stage => stage.StageId).SequenceEqual(request.RequiredStageIds) ||
            value.Stages.Any(stage => string.IsNullOrWhiteSpace(stage.ImmutableTaskReference) || !stage.UsesLockedDependencies) ||
            !value.Stages.SelectMany(stage => stage.RequiredControlIds).ToImmutableHashSet(StringComparer.Ordinal)
                .SetEquals(request.RequiredControlIds) || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Governed CI/CD workflow is invalid or mismatched.");
    }

    private static void ValidateWorkflowDecision(CiCdWorkflowValidationRequest request, CiCdWorkflowValidationDecision value)
    {
        if (value.ExecutionId != request.ExecutionId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.WorkflowSha256Digest, request.Workflow.Sha256Digest) || !value.IsAllowed ||
            value.MutableTaskReferenceDetected || value.DynamicSubstitutionDetected || value.UnapprovedCommandOrDownloadDetected ||
            value.UnauthorizedRegistryDetected || value.SecretMaterialDetected || value.ProductionCredentialDetected ||
            value.PrivilegedExecutionDetected || value.PolicyTamperingDetected || value.EvidenceReferences.IsDefaultOrEmpty ||
            value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Institutional CI/CD workflow validation denied or mismatched.");
    }

    private static void ValidateResult(InstitutionalCiCdExecutionRequest request, InstitutionalCiCdExecutionResult value)
    {
        value.OutputManifest.ManifestSha256Digest.ValidateDigestLabel("pipeline output manifest");
        if (value.ExecutionId != request.ExecutionId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.Ordinal.Equals(value.RepositoryId, request.RepositoryId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CommitId, request.CommitId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.WorkflowSha256Digest, request.Workflow.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(value.RunnerPoolId, request.RunnerPoolId) || string.IsNullOrWhiteSpace(value.RunnerIdentity) ||
            !value.EphemeralIsolationVerified || !value.NetworkDefaultDeny || value.ProductionCredentialsPresent ||
            !value.LockedDependencyRestore || !value.CiCdTriggered || value.SourceMutationOccurred || value.PullRequestCreated ||
            value.MergeOccurred || value.TagCreated || value.ArtifactPublished || value.RegistryMutated ||
            value.DeploymentOccurred || value.ProductionEffectOccurred || value.EvidenceReferences.IsDefaultOrEmpty ||
            value.CompletedAt < request.RequestedAt)
            throw new InvalidOperationException("Institutional CI/CD gateway returned an invalid or unsafe execution proof.");
        if (!value.Stages.Select(stage => stage.StageId).SequenceEqual(request.RequiredStageIds) ||
            value.Stages.Any(stage => !stage.Completed || !stage.Succeeded || stage.Skipped || stage.TimedOut ||
                stage.EvidenceReferences.IsDefaultOrEmpty) ||
            !value.Stages.SelectMany(stage => stage.ControlIds).ToImmutableHashSet(StringComparer.Ordinal)
                .SetEquals(request.RequiredControlIds) ||
            value.Stages.SelectMany(stage => stage.ControlIds).GroupBy(id => id, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new InvalidOperationException("CI/CD stage or control result is incomplete, duplicated, or mismatched.");
        var manifest = value.OutputManifest;
        GovernedAiPlanningRequest.ValidateDigest(manifest.ManifestSha256Digest, "pipeline output manifest");
        GovernedAiPlanningRequest.ValidateDigest(manifest.TreeSha256Digest, "pipeline source tree");
        GovernedAiPlanningRequest.ValidateDigest(manifest.WorkflowSha256Digest, "pipeline workflow");
        GovernedAiPlanningRequest.ValidateDigest(manifest.DependencyLockSha256Digest, "dependency lock state");
        if (!StringComparer.Ordinal.Equals(manifest.RepositoryId, request.RepositoryId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(manifest.CommitId, request.CommitId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(manifest.TreeSha256Digest, request.TreeSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(manifest.WorkflowSha256Digest, request.Workflow.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(manifest.RunnerIdentity, value.RunnerIdentity) ||
            manifest.OutputSha256Digests.IsDefaultOrEmpty || manifest.OutputSha256Digests.Any(digest => !IsDigest(digest)) ||
            string.IsNullOrWhiteSpace(manifest.SbomReference) || string.IsNullOrWhiteSpace(manifest.ChecksumsReference) ||
            string.IsNullOrWhiteSpace(manifest.ProvenanceReference) || string.IsNullOrWhiteSpace(manifest.BuildAttestationReference) ||
            string.IsNullOrWhiteSpace(manifest.SignatureReference) || manifest.IsReleasedArtifact ||
            manifest.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("CI/CD pipeline output manifest is invalid or represents a released artifact.");
    }

    private static bool IsDigest(string value) =>
        value is not null && value.Length == 64 && value.All(Uri.IsHexDigit);

    private static void ValidateAuthorization(CiCdResultAuthorizationRequest request, CiCdResultAuthorizationDecision value)
    {
        if (value.AuthorizationRequestId != request.AuthorizationRequestId || value.ExecutionId != request.ExecutionId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ManifestSha256Digest, request.Result.OutputManifest.ManifestSha256Digest) ||
            !value.IsAllowed || string.IsNullOrWhiteSpace(value.Code) || value.EvidenceReferences.IsDefaultOrEmpty ||
            value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("CI/CD result authorization denied or mismatched.");
    }

    private static void ValidateEvidence(CiCdEvidenceRecord record, CiCdEvidenceReceipt value)
    {
        if (value.ExecutionId != record.ExecutionId || value.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ManifestSha256Digest, record.Result.OutputManifest.ManifestSha256Digest) ||
            string.IsNullOrWhiteSpace(value.EvidenceReference) || value.RecordedAt < record.AuthorizedAt)
            throw new InvalidOperationException("CI/CD evidence receipt is invalid.");
    }
}

internal static class CiCdDigestValidation
{
    internal static void ValidateDigestLabel(this string value, string label) =>
        GovernedAiPlanningRequest.ValidateDigest(value, label);
}
