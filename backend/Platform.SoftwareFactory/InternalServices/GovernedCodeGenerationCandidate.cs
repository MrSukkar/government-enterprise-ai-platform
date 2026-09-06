using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedCodeGenerationRequest(
    Guid GenerationId,
    Guid PlanningId,
    Guid PackageSelectionId,
    Guid DeliveryRunId,
    string ExpectedSelectionSha256Digest,
    string ExpectedPlanningSha256Digest,
    string PromptTemplateId,
    string PromptTemplateVersion,
    string RuntimeProfile,
    ImmutableArray<string> ContextReferences,
    ImmutableArray<string> Constraints,
    ImmutableArray<string> RequestedOutputPaths,
    GovernedIdentity Identity,
    string Purpose,
    DataClassification MaximumClassification,
    string AuthorizationEvidenceReference,
    string Environment,
    IntentPolicyBundleReference PolicyBundle,
    DateTimeOffset RequestedAt)
{
    public GovernedCodeGenerationRequest Validate()
    {
        if (GenerationId == Guid.Empty || PlanningId == Guid.Empty || PackageSelectionId == Guid.Empty || DeliveryRunId == Guid.Empty)
            throw new InvalidOperationException("Code Generation and prerequisite identities are required.");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedSelectionSha256Digest, "package selection");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedPlanningSha256Digest, "AI Planning candidate");
        ArgumentException.ThrowIfNullOrWhiteSpace(PromptTemplateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(PromptTemplateVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(RuntimeProfile);
        ValidateUniqueValues(ContextReferences, "Authorized Code Generation context references are required.", requireOne: true);
        ValidateUniqueValues(Constraints, "Code Generation constraints must be unique and non-empty.", requireOne: false);
        ValidateUniqueValues(RequestedOutputPaths, "One or more unique output paths are required.", requireOne: true);
        foreach (var path in RequestedOutputPaths) GovernedGeneratedPath.Validate(path);
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("developer.internal-service.code-generation.create"))
            throw new UnauthorizedAccessException("Governed Code Generation permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Code Generation.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentNullException.ThrowIfNull(PolicyBundle);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Code Generation policy environment or time is invalid.");
        return this;
    }

    private static void ValidateUniqueValues(ImmutableArray<string> values, string message, bool requireOne)
    {
        if (values.IsDefault || requireOne && values.IsEmpty || values.Any(string.IsNullOrWhiteSpace) ||
            values.Distinct(StringComparer.Ordinal).Count() != values.Length)
            throw new InvalidOperationException(message);
    }
}

public interface IAuthorizedAiPlanningCandidateReader
{
    Task<GovernedAiPlanningReceipt?> LoadAsync(Guid planningId, string tenantId, CancellationToken cancellationToken);
}

public interface ICodeGenerationDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken);
}

public sealed record GovernedCodeGenerationPromptTemplate(
    string TemplateId,
    string Version,
    string Sha256Digest,
    string Content,
    ImmutableHashSet<string> AllowedTenantIds,
    ImmutableHashSet<string> AllowedPurposes,
    ImmutableHashSet<string> AllowedEnvironments,
    DataClassification MaximumClassification,
    bool IsCodeGenerationApproved,
    bool SignatureValid,
    string SignatureEvidenceReference,
    bool IsActive,
    DateTimeOffset ApprovedAt);

public interface IGovernedCodeGenerationPromptTemplateReader
{
    Task<GovernedCodeGenerationPromptTemplate?> LoadExactAsync(string templateId, string version, CancellationToken cancellationToken);
}

public sealed record CodeGenerationPolicyInput(
    Guid DecisionRequestId, Guid GenerationId, Guid PlanningId, Guid PackageSelectionId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, string SelectionSha256Digest, string PlanningSha256Digest,
    string PromptTemplateId, string PromptTemplateVersion, string RuntimeProfile,
    ImmutableArray<string> ContextReferences, ImmutableArray<PackageCoordinate> ApprovedPackages,
    ImmutableArray<string> Constraints, ImmutableArray<string> RequestedOutputPaths,
    IntentPolicyBundleReference PolicyBundle, ImmutableArray<string> EvidenceReferences, DateTimeOffset EvaluatedAt);

public sealed record CodeGenerationPolicyDecision(
    Guid DecisionRequestId, Guid GenerationId, Guid PlanningId, Guid PackageSelectionId, Guid DeliveryRunId,
    string TenantId, string Environment, string SelectionSha256Digest, string PlanningSha256Digest,
    string BundleId, string BundleVersion, string BundleSha256Digest,
    bool PolicySignatureValid, string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome, DataClassification MaximumClassification,
    string AllowedPromptTemplateId, string AllowedPromptTemplateVersion, string AllowedPromptSha256Digest,
    string AllowedRuntimeProfile, ImmutableHashSet<string> AllowedContextReferences,
    ImmutableHashSet<PackageCoordinate> AllowedPackages, ImmutableHashSet<string> AllowedConstraints,
    ImmutableHashSet<string> AllowedOutputPaths, ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ICodeGenerationPolicyGate
{
    Task<CodeGenerationPolicyDecision> EvaluateAsync(CodeGenerationPolicyInput input, CancellationToken cancellationToken);
}

public sealed record CodeGenerationContextAuthorizationRequest(
    Guid AuthorizationRequestId, Guid GenerationId, string TenantId, string SubjectId,
    string Purpose, DataClassification MaximumClassification, string ContextReference,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record CodeGenerationContextAuthorizationDecision(
    Guid AuthorizationRequestId, Guid GenerationId, string TenantId, string ContextReference,
    bool IsAllowed, string Code, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ICodeGenerationContextAuthorizer
{
    Task<CodeGenerationContextAuthorizationDecision> AuthorizeAsync(
        CodeGenerationContextAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record CodeGenerationResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid GenerationId, Guid PlanningId, string TenantId, string SubjectId,
    string Purpose, string CandidateSha256Digest, string RuntimeProfile,
    ImmutableArray<string> GeneratedFilePaths, ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record CodeGenerationResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid GenerationId, Guid PlanningId, string TenantId,
    string CandidateSha256Digest, bool IsAllowed, string Code,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ICodeGenerationResultAuthorizer
{
    Task<CodeGenerationResultAuthorizationDecision> AuthorizeAsync(
        CodeGenerationResultAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record CodeGenerationEvidenceRecord(
    Guid GenerationId, Guid PlanningId, Guid PackageSelectionId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose, string SelectionSha256Digest,
    string PlanningSha256Digest, Guid PolicyDecisionRequestId, string PromptSha256Digest,
    string RuntimeProfile, string CandidateSha256Digest, AiCandidateArtifact Candidate,
    AiEvaluationReport Evaluation, ImmutableArray<string> EvidenceReferences, DateTimeOffset GeneratedAt);

public sealed record CodeGenerationEvidenceReceipt(
    Guid GenerationId, Guid PlanningId, Guid DeliveryRunId, string TenantId,
    string CandidateSha256Digest, string EvidenceReference, DateTimeOffset RecordedAt);

public interface ICodeGenerationEvidenceRecorder
{
    Task<CodeGenerationEvidenceReceipt> RecordAsync(CodeGenerationEvidenceRecord record, CancellationToken cancellationToken);
}

public sealed class CodeGenerationDependencyUnavailableException(string message) : Exception(message);

public sealed record GovernedCodeGenerationReceipt(
    Guid GenerationId, Guid PlanningId, Guid PackageSelectionId, Guid DeliveryRunId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, bool IsCodeCandidateReleased, bool IsExecutable,
    bool IsApplied, bool CanAdvance, string? CandidateSha256Digest, string? Content,
    ImmutableArray<string> GeneratedFilePaths, ImmutableArray<AiEvaluationFinding> EvaluationFindings,
    string? GenerationEvidenceReference, ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedCodeGenerationEngine
{
    public async Task<GovernedCodeGenerationReceipt> GenerateAsync(
        GovernedCodeGenerationRequest request,
        IAuthorizedAiPlanningCandidateReader planningReader,
        IAuthorizedApprovedPackagesSnapshotReader packagesReader,
        ICodeGenerationDeliveryRunReader runReader,
        ICodeGenerationPolicyGate policyGate,
        IGovernedCodeGenerationPromptTemplateReader promptReader,
        ICodeGenerationContextAuthorizer contextAuthorizer,
        IAiDevelopmentRuntime runtime,
        IAiOutputEvaluator evaluator,
        ICodeGenerationResultAuthorizer resultAuthorizer,
        ICodeGenerationEvidenceRecorder evidenceRecorder,
        CancellationToken cancellationToken)
    {
        request.Validate();
        var planning = await planningReader.LoadAsync(request.PlanningId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed AI Planning candidate was not found.");
        ValidatePlanning(request, planning);
        var packages = await packagesReader.LoadAsync(request.PackageSelectionId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Approved Packages snapshot was not found.");
        ValidatePackages(request, planning, packages);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);

        if (!request.ContextReferences.Contains(planning.PlanningEvidenceReference!, StringComparer.Ordinal))
            throw new UnauthorizedAccessException("The authorized context must include the AI Planning evidence reference.");

        var prerequisiteEvidence = planning.EvidenceReferences.Append(planning.PlanningEvidenceReference!)
            .Concat(packages.EvidenceReferences).Append(packages.SelectionEvidenceReference!)
            .Append(run.History[^1].EvidenceReference).Append(request.AuthorizationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var input = new CodeGenerationPolicyInput(
            Guid.NewGuid(), request.GenerationId, planning.PlanningId, packages.SelectionId, run.Id,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, packages.SelectionSha256Digest!, planning.CandidateSha256Digest!,
            request.PromptTemplateId, request.PromptTemplateVersion, request.RuntimeProfile,
            request.ContextReferences, packages.Packages.Select(item => item.Coordinate).ToImmutableArray(),
            request.Constraints, request.RequestedOutputPaths, request.PolicyBundle, prerequisiteEvidence, request.RequestedAt);
        var decision = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidateDecision(input, request.Identity, decision);
        var policyEvidence = prerequisiteEvidence.Append(decision.PolicyVerificationEvidenceReference)
            .Concat(decision.EvidenceReferences).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        if (decision.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedCodeGenerationReceipt(
                request.GenerationId, planning.PlanningId, packages.SelectionId, run.Id, request.Identity.TenantId,
                decision.Outcome, false, false, false, false, null, null, [], [], null, policyEvidence,
                "Policy denial requires a new governed Code Generation request", decision.DecidedAt);

        var prompt = await promptReader.LoadExactAsync(request.PromptTemplateId, request.PromptTemplateVersion, cancellationToken)
            ?? throw new CodeGenerationDependencyUnavailableException("The exact governed Code Generation prompt is unavailable.");
        ValidatePrompt(request, decision, prompt);

        var contextEvidence = ImmutableArray.CreateBuilder<string>();
        foreach (var reference in request.ContextReferences.Order(StringComparer.Ordinal))
        {
            var authorizationRequest = new CodeGenerationContextAuthorizationRequest(
                Guid.NewGuid(), request.GenerationId, request.Identity.TenantId, request.Identity.SubjectId,
                request.Purpose, decision.MaximumClassification, reference, policyEvidence, decision.DecidedAt);
            var authorization = await contextAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
            ValidateContextAuthorization(authorizationRequest, authorization);
            contextEvidence.AddRange(authorization.EvidenceReferences);
        }

        var aiRequest = new AiDevelopmentRequest(
            run, AiDevelopmentTaskKind.CodeGeneration, request.Purpose,
            $"{prompt.TemplateId}@{prompt.Version}:{prompt.Sha256Digest}",
            request.ContextReferences.Order(StringComparer.Ordinal).ToImmutableArray(),
            packages.Packages.Select(item => item.Coordinate).OrderBy(item => item.Name, StringComparer.Ordinal).ToImmutableArray(),
            request.Constraints.Order(StringComparer.Ordinal).ToImmutableArray());
        var evaluated = await new GovernedAiDevelopmentService(runtime, evaluator)
            .ProduceCandidateAsync(aiRequest, cancellationToken);
        ValidateCandidate(request, decision, evaluated);
        var digest = Digest(packages.SelectionSha256Digest!, planning.CandidateSha256Digest!, prompt.Sha256Digest, evaluated);

        var evaluationEvidence = evaluated.Evaluation.Findings.Select(item => item.EvidenceReference)
            .Concat(contextEvidence).Concat(policyEvidence).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        var resultRequest = new CodeGenerationResultAuthorizationRequest(
            Guid.NewGuid(), request.GenerationId, planning.PlanningId, request.Identity.TenantId,
            request.Identity.SubjectId, request.Purpose, digest, evaluated.Candidate.RuntimeProfile,
            evaluated.Candidate.GeneratedFilePaths.Order(StringComparer.Ordinal).ToImmutableArray(),
            evaluationEvidence, evaluated.Evaluation.EvaluatedAt);
        var resultDecision = await resultAuthorizer.AuthorizeAsync(resultRequest, cancellationToken);
        ValidateResultAuthorization(resultRequest, resultDecision);
        var allEvidence = evaluationEvidence.Concat(resultDecision.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new CodeGenerationEvidenceRecord(
            request.GenerationId, planning.PlanningId, packages.SelectionId, run.Id,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose,
            packages.SelectionSha256Digest!, planning.CandidateSha256Digest!, decision.DecisionRequestId,
            prompt.Sha256Digest, evaluated.Candidate.RuntimeProfile, digest, evaluated.Candidate,
            evaluated.Evaluation, allEvidence, resultDecision.DecidedAt);
        var evidenceReceipt = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidenceReceipt(record, evidenceReceipt);

        return new GovernedCodeGenerationReceipt(
            request.GenerationId, planning.PlanningId, packages.SelectionId, run.Id, request.Identity.TenantId,
            decision.Outcome, true, IsExecutable: false, IsApplied: false, CanAdvance: false,
            digest, evaluated.Candidate.Content,
            evaluated.Candidate.GeneratedFilePaths.Order(StringComparer.Ordinal).ToImmutableArray(),
            evaluated.Evaluation.Findings, evidenceReceipt.EvidenceReference,
            allEvidence.Append(evidenceReceipt.EvidenceReference).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved Static Validation", evidenceReceipt.RecordedAt);
    }

    private static void ValidatePlanning(GovernedCodeGenerationRequest request, GovernedAiPlanningReceipt planning)
    {
        if (planning.PlanningId != request.PlanningId || planning.PackageSelectionId != request.PackageSelectionId ||
            planning.DeliveryRunId != request.DeliveryRunId || !StringComparer.Ordinal.Equals(planning.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(planning.CandidateSha256Digest, request.ExpectedPlanningSha256Digest))
            throw new InvalidOperationException("AI Planning candidate does not match the Code Generation request.");
        if (planning.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !planning.IsPlanningCandidateReleased ||
            planning.IsExecutable || planning.CanAdvance || string.IsNullOrWhiteSpace(planning.Content) ||
            string.IsNullOrWhiteSpace(planning.PlanningEvidenceReference) || planning.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("AI Planning candidate is not eligible for Code Generation.");
    }

    private static void ValidatePackages(
        GovernedCodeGenerationRequest request, GovernedAiPlanningReceipt planning,
        GovernedApprovedPackagesSelectionReceipt packages)
    {
        if (packages.SelectionId != request.PackageSelectionId || planning.PackageSelectionId != packages.SelectionId ||
            !StringComparer.Ordinal.Equals(packages.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(packages.SelectionSha256Digest, request.ExpectedSelectionSha256Digest))
            throw new InvalidOperationException("Approved Packages snapshot does not match the Code Generation request.");
        if (packages.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !packages.IsSelectionReleased || packages.CanAdvance ||
            packages.Packages.IsDefaultOrEmpty || string.IsNullOrWhiteSpace(packages.SelectionEvidenceReference))
            throw new UnauthorizedAccessException("Approved Packages snapshot is not eligible for Code Generation.");
    }

    private static void ValidateRun(GovernedCodeGenerationRequest request, SoftwareDeliveryRun run)
    {
        run.Validate();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.AiPlanning || run.History.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Code Generation requires a matching delivery run stopped at AiPlanning.");
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.AiPlanning).ToArray();
        if (run.History.Length != expected.Length || run.History.Where((item, index) =>
                item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) ||
                index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new InvalidOperationException("Software Delivery Run history is not a valid deterministic AiPlanning boundary.");
    }

    private static void ValidateDecision(
        CodeGenerationPolicyInput input, GovernedIdentity identity, CodeGenerationPolicyDecision decision)
    {
        if (decision.DecisionRequestId != input.DecisionRequestId || decision.GenerationId != input.GenerationId ||
            decision.PlanningId != input.PlanningId || decision.PackageSelectionId != input.PackageSelectionId ||
            decision.DeliveryRunId != input.DeliveryRunId || !StringComparer.Ordinal.Equals(decision.TenantId, input.TenantId) ||
            !StringComparer.Ordinal.Equals(decision.Environment, input.Environment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.SelectionSha256Digest, input.SelectionSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.PlanningSha256Digest, input.PlanningSha256Digest) ||
            !StringComparer.Ordinal.Equals(decision.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(decision.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Code Generation decision; invocation denied fail closed.");
        if (!decision.PolicySignatureValid || string.IsNullOrWhiteSpace(decision.PolicyVerificationEvidenceReference) ||
            decision.MaximumClassification > input.MaximumClassification || decision.MaximumClassification > identity.Clearance ||
            decision.Reasons.IsDefaultOrEmpty || decision.EvidenceReferences.IsDefaultOrEmpty || decision.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("Code Generation OPA signature, scope, reasons, or evidence is invalid.");
        if (decision.Outcome == GovernedIntentPolicyOutcome.Permit &&
            (!StringComparer.Ordinal.Equals(decision.AllowedPromptTemplateId, input.PromptTemplateId) ||
             !StringComparer.Ordinal.Equals(decision.AllowedPromptTemplateVersion, input.PromptTemplateVersion) ||
             !StringComparer.Ordinal.Equals(decision.AllowedRuntimeProfile, input.RuntimeProfile) ||
             !decision.AllowedContextReferences.SetEquals(input.ContextReferences) ||
             !decision.AllowedPackages.SetEquals(input.ApprovedPackages) ||
             !decision.AllowedConstraints.SetEquals(input.Constraints) ||
             !decision.AllowedOutputPaths.SetEquals(input.RequestedOutputPaths)))
            throw new UnauthorizedAccessException("OPA did not authorize the exact Code Generation input.");
    }

    private static void ValidatePrompt(
        GovernedCodeGenerationRequest request, CodeGenerationPolicyDecision decision,
        GovernedCodeGenerationPromptTemplate prompt)
    {
        GovernedAiPlanningRequest.ValidateDigest(prompt.Sha256Digest, "Code Generation prompt");
        if (!StringComparer.Ordinal.Equals(prompt.TemplateId, request.PromptTemplateId) ||
            !StringComparer.Ordinal.Equals(prompt.Version, request.PromptTemplateVersion) ||
            !StringComparer.OrdinalIgnoreCase.Equals(prompt.Sha256Digest, decision.AllowedPromptSha256Digest) ||
            string.IsNullOrWhiteSpace(prompt.Content) || !prompt.AllowedTenantIds.Contains(request.Identity.TenantId) ||
            !prompt.AllowedPurposes.Contains(request.Purpose) || !prompt.AllowedEnvironments.Contains(request.Environment) ||
            prompt.MaximumClassification < request.MaximumClassification || !prompt.IsCodeGenerationApproved ||
            !prompt.SignatureValid || string.IsNullOrWhiteSpace(prompt.SignatureEvidenceReference) ||
            !prompt.IsActive || prompt.ApprovedAt > decision.DecidedAt)
            throw new UnauthorizedAccessException("Governed Code Generation prompt is invalid or out of scope.");
    }

    private static void ValidateContextAuthorization(
        CodeGenerationContextAuthorizationRequest request, CodeGenerationContextAuthorizationDecision decision)
    {
        if (decision.AuthorizationRequestId != request.AuthorizationRequestId || decision.GenerationId != request.GenerationId ||
            !StringComparer.Ordinal.Equals(decision.TenantId, request.TenantId) ||
            !StringComparer.Ordinal.Equals(decision.ContextReference, request.ContextReference) || !decision.IsAllowed ||
            string.IsNullOrWhiteSpace(decision.Code) || decision.EvidenceReferences.IsDefaultOrEmpty ||
            decision.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Code Generation context re-authorization denied or mismatched.");
    }

    private static void ValidateCandidate(
        GovernedCodeGenerationRequest request, CodeGenerationPolicyDecision decision, EvaluatedAiCandidate evaluated)
    {
        if (evaluated.Candidate.GeneratedFilePaths.IsDefaultOrEmpty)
            throw new InvalidOperationException("Code Generation must return one or more generated file paths.");
        foreach (var path in evaluated.Candidate.GeneratedFilePaths) GovernedGeneratedPath.Validate(path);
        if (!StringComparer.Ordinal.Equals(evaluated.Candidate.RuntimeProfile, decision.AllowedRuntimeProfile) ||
            !evaluated.Candidate.ContextReferences.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(request.ContextReferences) ||
            !evaluated.Candidate.GeneratedFilePaths.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(request.RequestedOutputPaths) ||
            !decision.AllowedOutputPaths.SetEquals(evaluated.Candidate.GeneratedFilePaths) ||
            !evaluated.Evaluation.IsAccepted || evaluated.IsExecutable || !evaluated.IsEligibleForWorkflowEvidence ||
            evaluated.Evaluation.Findings.Any(item => string.IsNullOrWhiteSpace(item.Rationale) || string.IsNullOrWhiteSpace(item.EvidenceReference)))
            throw new UnauthorizedAccessException("Code Generation candidate or independent evaluation is invalid.");
    }

    private static string Digest(
        string selectionDigest, string planningDigest, string promptDigest, EvaluatedAiCandidate evaluated)
    {
        var value = $"{selectionDigest}|{planningDigest}|{promptDigest}|{evaluated.Candidate.InvocationId:D}|{evaluated.Candidate.RuntimeProfile}|{evaluated.Candidate.Content}|{string.Join(',', evaluated.Candidate.ContextReferences.Order(StringComparer.Ordinal))}|{string.Join(',', evaluated.Candidate.GeneratedFilePaths.Order(StringComparer.Ordinal))}|{string.Join(',', evaluated.Evaluation.Findings.OrderBy(item => item.Criterion).Select(item => $"{item.Criterion}:{item.Passed}:{item.EvidenceReference}"))}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static void ValidateResultAuthorization(
        CodeGenerationResultAuthorizationRequest request, CodeGenerationResultAuthorizationDecision decision)
    {
        if (decision.AuthorizationRequestId != request.AuthorizationRequestId || decision.GenerationId != request.GenerationId ||
            decision.PlanningId != request.PlanningId || !StringComparer.Ordinal.Equals(decision.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.CandidateSha256Digest, request.CandidateSha256Digest) ||
            !decision.IsAllowed || string.IsNullOrWhiteSpace(decision.Code) || decision.EvidenceReferences.IsDefaultOrEmpty ||
            decision.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Code Generation result authorization denied or mismatched.");
    }

    private static void ValidateEvidenceReceipt(CodeGenerationEvidenceRecord record, CodeGenerationEvidenceReceipt receipt)
    {
        if (receipt.GenerationId != record.GenerationId || receipt.PlanningId != record.PlanningId ||
            receipt.DeliveryRunId != record.DeliveryRunId || !StringComparer.Ordinal.Equals(receipt.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.CandidateSha256Digest, record.CandidateSha256Digest) ||
            string.IsNullOrWhiteSpace(receipt.EvidenceReference) || receipt.RecordedAt < record.GeneratedAt)
            throw new InvalidOperationException("Code Generation evidence recorder returned a mismatched receipt.");
    }
}

internal static class GovernedGeneratedPath
{
    private static readonly ImmutableHashSet<string> ProhibitedSegments =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, ".git", ".vs", "bin", "obj", "secrets");

    internal static void Validate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path != path.Trim() || path.Contains('\\') || path.Contains(':') || path.Any(char.IsControl) ||
            Path.IsPathFullyQualified(path) || Uri.TryCreate(path, UriKind.Absolute, out _))
            throw new InvalidOperationException("Generated paths must be normalized repository-relative paths.");
        var segments = path.Split('/', StringSplitOptions.None);
        if (segments.Length == 0 || segments.Any(segment => string.IsNullOrWhiteSpace(segment) ||
                segment is "." or ".." || ProhibitedSegments.Contains(segment)) ||
            segments[^1].Equals(".env", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Generated path escapes or targets a prohibited repository location.");
    }
}
