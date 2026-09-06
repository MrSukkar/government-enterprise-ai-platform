using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedAiPlanningRequest(
    Guid PlanningId,
    Guid PackageSelectionId,
    Guid ArchitectureDiscoveryId,
    Guid SystemsDiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    Guid DeliveryRunId,
    long ExpectedRegistrationVersion,
    string ExpectedArchitectureSha256Digest,
    string ExpectedSelectionSha256Digest,
    string PromptTemplateId,
    string PromptTemplateVersion,
    string RuntimeProfile,
    ImmutableArray<string> ContextReferences,
    ImmutableArray<string> Constraints,
    GovernedIdentity Identity,
    string Purpose,
    DataClassification MaximumClassification,
    string AuthorizationEvidenceReference,
    string Environment,
    IntentPolicyBundleReference PolicyBundle,
    DateTimeOffset RequestedAt)
{
    public GovernedAiPlanningRequest Validate()
    {
        if (PlanningId == Guid.Empty || PackageSelectionId == Guid.Empty || ArchitectureDiscoveryId == Guid.Empty ||
            SystemsDiscoveryId == Guid.Empty || ContextDiscoveryId == Guid.Empty || RegistrationId == Guid.Empty || DeliveryRunId == Guid.Empty)
            throw new InvalidOperationException("AI Planning and prerequisite identities are required.");
        if (ExpectedRegistrationVersion < 0) throw new InvalidOperationException("A registration version is required.");
        ValidateDigest(ExpectedArchitectureSha256Digest, "architecture");
        ValidateDigest(ExpectedSelectionSha256Digest, "package selection");
        ArgumentException.ThrowIfNullOrWhiteSpace(PromptTemplateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(PromptTemplateVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(RuntimeProfile);
        if (ContextReferences.IsDefaultOrEmpty || ContextReferences.Any(string.IsNullOrWhiteSpace) ||
            ContextReferences.Distinct(StringComparer.Ordinal).Count() != ContextReferences.Length)
            throw new InvalidOperationException("Unique authorized planning context references are required.");
        if (Constraints.IsDefault || Constraints.Any(string.IsNullOrWhiteSpace) ||
            Constraints.Distinct(StringComparer.Ordinal).Count() != Constraints.Length)
            throw new InvalidOperationException("Planning constraints must be unique and non-empty.");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("developer.internal-service.ai-planning.create"))
            throw new UnauthorizedAccessException("Governed AI Planning permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for AI Planning.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentNullException.ThrowIfNull(PolicyBundle);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("AI Planning policy environment or time is invalid.");
        return this;
    }

    internal static void ValidateDigest(string value, string owner)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException($"{owner} requires a SHA-256 digest.");
    }
}

public interface IAuthorizedApprovedPackagesSnapshotReader
{
    Task<GovernedApprovedPackagesSelectionReceipt?> LoadAsync(Guid selectionId, string tenantId, CancellationToken cancellationToken);
}

public interface IAiPlanningDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken);
}

public sealed record GovernedPlanningPromptTemplate(
    string TemplateId,
    string Version,
    string Sha256Digest,
    string Content,
    ImmutableHashSet<string> AllowedTenantIds,
    ImmutableHashSet<string> AllowedPurposes,
    ImmutableHashSet<string> AllowedEnvironments,
    DataClassification MaximumClassification,
    bool IsPlanningApproved,
    bool SignatureValid,
    string SignatureEvidenceReference,
    bool IsActive,
    DateTimeOffset ApprovedAt);

public interface IGovernedPlanningPromptTemplateReader
{
    Task<GovernedPlanningPromptTemplate?> LoadExactAsync(string templateId, string version, CancellationToken cancellationToken);
}

public sealed record AiPlanningPolicyInput(
    Guid DecisionRequestId, Guid PlanningId, Guid PackageSelectionId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, string SelectionSha256Digest,
    string PromptTemplateId, string PromptTemplateVersion, string RuntimeProfile,
    ImmutableArray<string> ContextReferences, ImmutableArray<PackageCoordinate> ApprovedPackages,
    ImmutableArray<string> Constraints, IntentPolicyBundleReference PolicyBundle,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset EvaluatedAt);

public sealed record AiPlanningPolicyDecision(
    Guid DecisionRequestId, Guid PlanningId, Guid PackageSelectionId, Guid DeliveryRunId,
    string TenantId, string Environment, string SelectionSha256Digest,
    string BundleId, string BundleVersion, string BundleSha256Digest,
    bool PolicySignatureValid, string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome, DataClassification MaximumClassification,
    string AllowedPromptTemplateId, string AllowedPromptTemplateVersion, string AllowedPromptSha256Digest,
    string AllowedRuntimeProfile, ImmutableHashSet<string> AllowedContextReferences,
    ImmutableHashSet<PackageCoordinate> AllowedPackages, ImmutableHashSet<string> AllowedConstraints,
    ImmutableArray<string> Reasons, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IAiPlanningPolicyGate
{
    Task<AiPlanningPolicyDecision> EvaluateAsync(AiPlanningPolicyInput input, CancellationToken cancellationToken);
}

public sealed record AiPlanningContextAuthorizationRequest(
    Guid AuthorizationRequestId, Guid PlanningId, string TenantId, string SubjectId,
    string Purpose, DataClassification MaximumClassification, string ContextReference,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record AiPlanningContextAuthorizationDecision(
    Guid AuthorizationRequestId, Guid PlanningId, string TenantId, string ContextReference,
    bool IsAllowed, string Code, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IAiPlanningContextAuthorizer
{
    Task<AiPlanningContextAuthorizationDecision> AuthorizeAsync(AiPlanningContextAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record AiPlanningResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid PlanningId, string TenantId, string SubjectId,
    string Purpose, string CandidateSha256Digest, string RuntimeProfile,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record AiPlanningResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid PlanningId, string TenantId, string CandidateSha256Digest,
    bool IsAllowed, string Code, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IAiPlanningResultAuthorizer
{
    Task<AiPlanningResultAuthorizationDecision> AuthorizeAsync(AiPlanningResultAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record AiPlanningEvidenceRecord(
    Guid PlanningId, Guid PackageSelectionId, Guid DeliveryRunId, string TenantId,
    string SubjectId, string Purpose, string SelectionSha256Digest, Guid PolicyDecisionRequestId,
    string PromptSha256Digest, string RuntimeProfile, string CandidateSha256Digest,
    AiCandidateArtifact Candidate, AiEvaluationReport Evaluation,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset PlannedAt);

public sealed record AiPlanningEvidenceReceipt(
    Guid PlanningId, Guid PackageSelectionId, Guid DeliveryRunId, string TenantId,
    string CandidateSha256Digest, string EvidenceReference, DateTimeOffset RecordedAt);

public interface IAiPlanningEvidenceRecorder
{
    Task<AiPlanningEvidenceReceipt> RecordAsync(AiPlanningEvidenceRecord record, CancellationToken cancellationToken);
}

public sealed class AiPlanningDependencyUnavailableException(string message) : Exception(message);

public sealed record GovernedAiPlanningReceipt(
    Guid PlanningId, Guid PackageSelectionId, Guid DeliveryRunId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, bool IsPlanningCandidateReleased,
    bool IsExecutable, bool CanAdvance, string? CandidateSha256Digest,
    string? Content, ImmutableArray<AiEvaluationFinding> EvaluationFindings,
    string? PlanningEvidenceReference, ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedAiPlanningEngine
{
    public async Task<GovernedAiPlanningReceipt> PlanAsync(
        GovernedAiPlanningRequest request,
        IAuthorizedApprovedPackagesSnapshotReader packagesReader,
        IAiPlanningDeliveryRunReader runReader,
        IAiPlanningPolicyGate policyGate,
        IGovernedPlanningPromptTemplateReader promptReader,
        IAiPlanningContextAuthorizer contextAuthorizer,
        IAiDevelopmentRuntime runtime,
        IAiOutputEvaluator evaluator,
        IAiPlanningResultAuthorizer resultAuthorizer,
        IAiPlanningEvidenceRecorder evidenceRecorder,
        CancellationToken cancellationToken)
    {
        request.Validate();
        var packages = await packagesReader.LoadAsync(request.PackageSelectionId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Approved Packages snapshot was not found.");
        ValidatePackages(request, packages);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);

        var prerequisiteEvidence = packages.EvidenceReferences.Append(packages.SelectionEvidenceReference!)
            .Append(run.History[^1].EvidenceReference).Append(request.AuthorizationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var input = new AiPlanningPolicyInput(
            Guid.NewGuid(), request.PlanningId, packages.SelectionId, run.Id,
            packages.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, packages.SelectionSha256Digest!, request.PromptTemplateId,
            request.PromptTemplateVersion, request.RuntimeProfile, request.ContextReferences,
            packages.Packages.Select(item => item.Coordinate).ToImmutableArray(), request.Constraints,
            request.PolicyBundle, prerequisiteEvidence, request.RequestedAt);
        var decision = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidateDecision(input, request.Identity, decision);
        var policyEvidence = prerequisiteEvidence.Append(decision.PolicyVerificationEvidenceReference)
            .Concat(decision.EvidenceReferences).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        if (decision.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedAiPlanningReceipt(request.PlanningId, packages.SelectionId, run.Id, packages.TenantId,
                decision.Outcome, false, false, false, null, null, [], null, policyEvidence,
                "Policy denial requires a new governed AI Planning request", decision.DecidedAt);

        var prompt = await promptReader.LoadExactAsync(request.PromptTemplateId, request.PromptTemplateVersion, cancellationToken)
            ?? throw new AiPlanningDependencyUnavailableException("The exact governed planning prompt template is unavailable.");
        ValidatePrompt(request, decision, prompt);

        var contextEvidence = ImmutableArray.CreateBuilder<string>();
        foreach (var reference in request.ContextReferences.Order(StringComparer.Ordinal))
        {
            var authorizationRequest = new AiPlanningContextAuthorizationRequest(
                Guid.NewGuid(), request.PlanningId, packages.TenantId, request.Identity.SubjectId,
                request.Purpose, decision.MaximumClassification, reference, policyEvidence, decision.DecidedAt);
            var authorization = await contextAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
            ValidateContextAuthorization(authorizationRequest, authorization);
            contextEvidence.AddRange(authorization.EvidenceReferences);
        }

        var aiRequest = new AiDevelopmentRequest(
            run, AiDevelopmentTaskKind.Planning, request.Purpose,
            $"{prompt.TemplateId}@{prompt.Version}:{prompt.Sha256Digest}",
            request.ContextReferences.Order(StringComparer.Ordinal).ToImmutableArray(),
            packages.Packages.Select(item => item.Coordinate).OrderBy(item => item.Name, StringComparer.Ordinal).ToImmutableArray(),
            request.Constraints.Order(StringComparer.Ordinal).ToImmutableArray());
        var evaluated = await new GovernedAiDevelopmentService(runtime, evaluator).ProduceCandidateAsync(aiRequest, cancellationToken);
        ValidateCandidate(request, decision, evaluated);
        var digest = Digest(packages.SelectionSha256Digest!, prompt.Sha256Digest, evaluated);

        var evaluationEvidence = evaluated.Evaluation.Findings.Select(item => item.EvidenceReference)
            .Concat(contextEvidence).Concat(policyEvidence).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        var resultRequest = new AiPlanningResultAuthorizationRequest(
            Guid.NewGuid(), request.PlanningId, packages.TenantId, request.Identity.SubjectId,
            request.Purpose, digest, evaluated.Candidate.RuntimeProfile, evaluationEvidence,
            evaluated.Evaluation.EvaluatedAt);
        var resultDecision = await resultAuthorizer.AuthorizeAsync(resultRequest, cancellationToken);
        ValidateResultAuthorization(resultRequest, resultDecision);
        var allEvidence = evaluationEvidence.Concat(resultDecision.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new AiPlanningEvidenceRecord(
            request.PlanningId, packages.SelectionId, run.Id, packages.TenantId,
            request.Identity.SubjectId, request.Purpose, packages.SelectionSha256Digest!,
            decision.DecisionRequestId, prompt.Sha256Digest, evaluated.Candidate.RuntimeProfile,
            digest, evaluated.Candidate, evaluated.Evaluation, allEvidence, resultDecision.DecidedAt);
        var evidenceReceipt = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidenceReceipt(record, evidenceReceipt);

        return new GovernedAiPlanningReceipt(
            request.PlanningId, packages.SelectionId, run.Id, packages.TenantId, decision.Outcome,
            true, IsExecutable: false, CanAdvance: false, digest, evaluated.Candidate.Content,
            evaluated.Evaluation.Findings, evidenceReceipt.EvidenceReference,
            allEvidence.Append(evidenceReceipt.EvidenceReference).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved Code Generation", evidenceReceipt.RecordedAt);
    }

    private static void ValidatePackages(GovernedAiPlanningRequest request, GovernedApprovedPackagesSelectionReceipt packages)
    {
        if (packages.SelectionId != request.PackageSelectionId || packages.ArchitectureDiscoveryId != request.ArchitectureDiscoveryId ||
            packages.SystemsDiscoveryId != request.SystemsDiscoveryId || packages.ContextDiscoveryId != request.ContextDiscoveryId ||
            packages.RegistrationId != request.RegistrationId || packages.RegistrationVersion != request.ExpectedRegistrationVersion ||
            !StringComparer.Ordinal.Equals(packages.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(packages.ArchitectureSha256Digest, request.ExpectedArchitectureSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(packages.SelectionSha256Digest, request.ExpectedSelectionSha256Digest))
            throw new InvalidOperationException("Approved Packages snapshot does not match the AI Planning request.");
        if (packages.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !packages.IsSelectionReleased || packages.CanAdvance ||
            packages.Packages.IsDefaultOrEmpty || string.IsNullOrWhiteSpace(packages.SelectionEvidenceReference))
            throw new UnauthorizedAccessException("Approved Packages snapshot is not eligible for AI Planning.");
    }

    private static void ValidateRun(GovernedAiPlanningRequest request, SoftwareDeliveryRun run)
    {
        run.Validate();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.ApprovedPackages || run.History.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("AI Planning requires a matching delivery run stopped at ApprovedPackages.");
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.ApprovedPackages).ToArray();
        if (run.History.Length != expected.Length || run.History.Where((item, index) =>
                item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) ||
                index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new InvalidOperationException("Software Delivery Run history is not a valid deterministic ApprovedPackages boundary.");
    }

    private static void ValidateDecision(AiPlanningPolicyInput input, GovernedIdentity identity, AiPlanningPolicyDecision decision)
    {
        if (decision.DecisionRequestId != input.DecisionRequestId || decision.PlanningId != input.PlanningId ||
            decision.PackageSelectionId != input.PackageSelectionId || decision.DeliveryRunId != input.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(decision.TenantId, input.TenantId) || !StringComparer.Ordinal.Equals(decision.Environment, input.Environment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.SelectionSha256Digest, input.SelectionSha256Digest) ||
            !StringComparer.Ordinal.Equals(decision.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(decision.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched AI Planning decision; invocation denied fail closed.");
        if (!decision.PolicySignatureValid || string.IsNullOrWhiteSpace(decision.PolicyVerificationEvidenceReference) ||
            decision.MaximumClassification > input.MaximumClassification || decision.MaximumClassification > identity.Clearance ||
            decision.Reasons.IsDefaultOrEmpty || decision.EvidenceReferences.IsDefaultOrEmpty || decision.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("AI Planning OPA signature, scope, reasons, or evidence is invalid.");
        if (decision.Outcome == GovernedIntentPolicyOutcome.Permit &&
            (!StringComparer.Ordinal.Equals(decision.AllowedPromptTemplateId, input.PromptTemplateId) ||
             !StringComparer.Ordinal.Equals(decision.AllowedPromptTemplateVersion, input.PromptTemplateVersion) ||
             !StringComparer.Ordinal.Equals(decision.AllowedRuntimeProfile, input.RuntimeProfile) ||
             !decision.AllowedContextReferences.SetEquals(input.ContextReferences) ||
             !decision.AllowedPackages.SetEquals(input.ApprovedPackages) || !decision.AllowedConstraints.SetEquals(input.Constraints)))
            throw new UnauthorizedAccessException("OPA did not authorize the exact AI Planning input.");
    }

    private static void ValidatePrompt(GovernedAiPlanningRequest request, AiPlanningPolicyDecision decision, GovernedPlanningPromptTemplate prompt)
    {
        GovernedAiPlanningRequest.ValidateDigest(prompt.Sha256Digest, "planning prompt");
        if (!StringComparer.Ordinal.Equals(prompt.TemplateId, request.PromptTemplateId) ||
            !StringComparer.Ordinal.Equals(prompt.Version, request.PromptTemplateVersion) ||
            !StringComparer.OrdinalIgnoreCase.Equals(prompt.Sha256Digest, decision.AllowedPromptSha256Digest) ||
            string.IsNullOrWhiteSpace(prompt.Content) || !prompt.AllowedTenantIds.Contains(request.Identity.TenantId) ||
            !prompt.AllowedPurposes.Contains(request.Purpose) || !prompt.AllowedEnvironments.Contains(request.Environment) ||
            prompt.MaximumClassification < request.MaximumClassification || !prompt.IsPlanningApproved ||
            !prompt.SignatureValid || string.IsNullOrWhiteSpace(prompt.SignatureEvidenceReference) || !prompt.IsActive || prompt.ApprovedAt > decision.DecidedAt)
            throw new UnauthorizedAccessException("Governed planning prompt template is invalid or out of scope.");
    }

    private static void ValidateContextAuthorization(AiPlanningContextAuthorizationRequest request, AiPlanningContextAuthorizationDecision decision)
    {
        if (decision.AuthorizationRequestId != request.AuthorizationRequestId || decision.PlanningId != request.PlanningId ||
            !StringComparer.Ordinal.Equals(decision.TenantId, request.TenantId) ||
            !StringComparer.Ordinal.Equals(decision.ContextReference, request.ContextReference) || !decision.IsAllowed ||
            string.IsNullOrWhiteSpace(decision.Code) || decision.EvidenceReferences.IsDefaultOrEmpty || decision.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("AI context re-authorization denied or mismatched.");
    }

    private static void ValidateCandidate(GovernedAiPlanningRequest request, AiPlanningPolicyDecision decision, EvaluatedAiCandidate evaluated)
    {
        if (!StringComparer.Ordinal.Equals(evaluated.Candidate.RuntimeProfile, decision.AllowedRuntimeProfile) ||
            evaluated.Candidate.GeneratedFilePaths.IsDefault == false && !evaluated.Candidate.GeneratedFilePaths.IsEmpty ||
            !evaluated.Candidate.ContextReferences.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(request.ContextReferences) ||
            !evaluated.Evaluation.IsAccepted || evaluated.IsExecutable || !evaluated.IsEligibleForWorkflowEvidence ||
            evaluated.Evaluation.Findings.Any(item => string.IsNullOrWhiteSpace(item.Rationale) || string.IsNullOrWhiteSpace(item.EvidenceReference)))
            throw new UnauthorizedAccessException("AI Planning candidate or independent evaluation is invalid.");
    }

    private static string Digest(string selectionDigest, string promptDigest, EvaluatedAiCandidate evaluated)
    {
        var value = $"{selectionDigest}|{promptDigest}|{evaluated.Candidate.InvocationId:D}|{evaluated.Candidate.RuntimeProfile}|{evaluated.Candidate.Content}|{string.Join(',', evaluated.Candidate.ContextReferences.Order(StringComparer.Ordinal))}|{string.Join(',', evaluated.Evaluation.Findings.OrderBy(item => item.Criterion).Select(item => $"{item.Criterion}:{item.Passed}:{item.EvidenceReference}"))}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static void ValidateResultAuthorization(AiPlanningResultAuthorizationRequest request, AiPlanningResultAuthorizationDecision decision)
    {
        if (decision.AuthorizationRequestId != request.AuthorizationRequestId || decision.PlanningId != request.PlanningId ||
            !StringComparer.Ordinal.Equals(decision.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.CandidateSha256Digest, request.CandidateSha256Digest) ||
            !decision.IsAllowed || string.IsNullOrWhiteSpace(decision.Code) || decision.EvidenceReferences.IsDefaultOrEmpty ||
            decision.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("AI Planning result authorization denied or mismatched.");
    }

    private static void ValidateEvidenceReceipt(AiPlanningEvidenceRecord record, AiPlanningEvidenceReceipt receipt)
    {
        if (receipt.PlanningId != record.PlanningId || receipt.PackageSelectionId != record.PackageSelectionId ||
            receipt.DeliveryRunId != record.DeliveryRunId || !StringComparer.Ordinal.Equals(receipt.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.CandidateSha256Digest, record.CandidateSha256Digest) ||
            string.IsNullOrWhiteSpace(receipt.EvidenceReference) || receipt.RecordedAt < record.PlannedAt)
            throw new InvalidOperationException("AI Planning evidence recorder returned a mismatched receipt.");
    }
}
