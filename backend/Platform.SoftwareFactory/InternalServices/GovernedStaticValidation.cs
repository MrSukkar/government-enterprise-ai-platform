using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.Validation;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedStaticValidationRequest(
    Guid ValidationId,
    Guid GenerationId,
    Guid DeliveryRunId,
    string ExpectedCandidateSha256Digest,
    string ExpectedGenerationEvidenceReference,
    ImmutableArray<string> RequiredControlIds,
    GovernedIdentity Identity,
    string Purpose,
    DataClassification MaximumClassification,
    string AuthorizationEvidenceReference,
    string Environment,
    IntentPolicyBundleReference PolicyBundle,
    DateTimeOffset RequestedAt)
{
    public GovernedStaticValidationRequest Validate()
    {
        if (ValidationId == Guid.Empty || GenerationId == Guid.Empty || DeliveryRunId == Guid.Empty)
            throw new InvalidOperationException("Static Validation and prerequisite identities are required.");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedCandidateSha256Digest, "code candidate");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedGenerationEvidenceReference);
        if (RequiredControlIds.IsDefaultOrEmpty || RequiredControlIds.Any(string.IsNullOrWhiteSpace) ||
            RequiredControlIds.Distinct(StringComparer.Ordinal).Count() != RequiredControlIds.Length)
            throw new InvalidOperationException("Unique required Static control identities are required.");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("developer.internal-service.static-validation.create"))
            throw new UnauthorizedAccessException("Governed Static Validation permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Static Validation.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentNullException.ThrowIfNull(PolicyBundle);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Static Validation policy environment or time is invalid.");
        return this;
    }
}

public sealed record AuthorizedCodeGenerationCandidateSnapshot(
    GovernedCodeGenerationReceipt Receipt,
    AiCandidateArtifact Candidate);

public interface IAuthorizedCodeGenerationCandidateReader
{
    Task<AuthorizedCodeGenerationCandidateSnapshot?> LoadAsync(
        Guid generationId, string tenantId, CancellationToken cancellationToken);
}

public interface IStaticValidationDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken);
}

public sealed record StaticValidationPolicyInput(
    Guid DecisionRequestId, Guid ValidationId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, string CandidateSha256Digest,
    string GenerationEvidenceReference, ImmutableArray<string> RequiredControlIds,
    IntentPolicyBundleReference PolicyBundle, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset EvaluatedAt);

public sealed record StaticValidationPolicyDecision(
    Guid DecisionRequestId, Guid ValidationId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, string Environment, string CandidateSha256Digest,
    string BundleId, string BundleVersion, string BundleSha256Digest,
    bool PolicySignatureValid, string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome, DataClassification MaximumClassification,
    ImmutableHashSet<string> AllowedControlIds, ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IStaticValidationPolicyGate
{
    Task<StaticValidationPolicyDecision> EvaluateAsync(
        StaticValidationPolicyInput input, CancellationToken cancellationToken);
}

public sealed record StaticValidationResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid ValidationId, Guid GenerationId, string TenantId,
    string SubjectId, string Purpose, string CandidateSha256Digest, string ReportSha256Digest,
    ImmutableArray<string> ControlIds, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RequestedAt);

public sealed record StaticValidationResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid ValidationId, Guid GenerationId, string TenantId,
    string CandidateSha256Digest, string ReportSha256Digest, bool IsAllowed, string Code,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IStaticValidationResultAuthorizer
{
    Task<StaticValidationResultAuthorizationDecision> AuthorizeAsync(
        StaticValidationResultAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record StaticValidationEvidenceRecord(
    Guid ValidationId, Guid GenerationId, Guid DeliveryRunId, string TenantId,
    string SubjectId, string Purpose, string CandidateSha256Digest,
    Guid PolicyDecisionRequestId, ValidationGateReport Report, string ReportSha256Digest,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset ValidatedAt);

public sealed record StaticValidationEvidenceReceipt(
    Guid ValidationId, Guid GenerationId, Guid DeliveryRunId, string TenantId,
    string CandidateSha256Digest, string ReportSha256Digest,
    string EvidenceReference, DateTimeOffset RecordedAt);

public interface IStaticValidationEvidenceRecorder
{
    Task<StaticValidationEvidenceReceipt> RecordAsync(
        StaticValidationEvidenceRecord record, CancellationToken cancellationToken);
}

public sealed class StaticValidationDependencyUnavailableException(string message) : Exception(message);

public sealed record GovernedStaticValidationReceipt(
    Guid ValidationId, Guid GenerationId, Guid DeliveryRunId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, ValidationGate Gate, bool IsAccepted,
    bool IsExecutable, bool CanAdvance, string CandidateSha256Digest,
    string? ReportSha256Digest, ImmutableArray<ValidationControlReport> ControlReports,
    string? ValidationEvidenceReference, ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedStaticValidationEngine
{
    public async Task<GovernedStaticValidationReceipt> ValidateAsync(
        GovernedStaticValidationRequest request,
        IStaticValidationPolicyGate policyGate,
        IAuthorizedCodeGenerationCandidateReader candidateReader,
        IStaticValidationDeliveryRunReader runReader,
        IEnumerable<ICodeValidationControl> controls,
        IStaticValidationResultAuthorizer resultAuthorizer,
        IStaticValidationEvidenceRecorder evidenceRecorder,
        CancellationToken cancellationToken)
    {
        request.Validate();
        var initialEvidence = ImmutableArray.Create(
            request.ExpectedGenerationEvidenceReference,
            request.AuthorizationEvidenceReference);
        var input = new StaticValidationPolicyInput(
            Guid.NewGuid(), request.ValidationId, request.GenerationId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, request.ExpectedCandidateSha256Digest,
            request.ExpectedGenerationEvidenceReference,
            request.RequiredControlIds.Order(StringComparer.Ordinal).ToImmutableArray(),
            request.PolicyBundle, initialEvidence, request.RequestedAt);
        var decision = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidateDecision(input, request.Identity, decision);
        var policyEvidence = initialEvidence.Append(decision.PolicyVerificationEvidenceReference)
            .Concat(decision.EvidenceReferences).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        if (decision.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedStaticValidationReceipt(
                request.ValidationId, request.GenerationId, request.DeliveryRunId,
                request.Identity.TenantId, decision.Outcome, ValidationGate.Static,
                false, false, false, request.ExpectedCandidateSha256Digest, null, [], null,
                policyEvidence, "Policy denial requires a new governed Static Validation request", decision.DecidedAt);

        var snapshot = await candidateReader.LoadAsync(
            request.GenerationId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Code Generation candidate was not found.");
        ValidateCandidate(request, snapshot);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);

        var selectedControls = ValidateControls(request, decision, controls);
        var report = await new CodeValidationPipeline(selectedControls).ExecuteAsync(
            new CodeValidationRequest(run, snapshot.Candidate, ValidationGate.Static), cancellationToken);
        ValidateReport(request, report);
        var reportDigest = Digest(request.ExpectedCandidateSha256Digest, report);
        var reportEvidence = report.ControlReports.Select(item => item.EvidenceReference)
            .Concat(report.ControlReports.SelectMany(item => item.Findings).Select(item => item.EvidenceReference))
            .Concat(snapshot.Receipt.EvidenceReferences).Append(snapshot.Receipt.GenerationEvidenceReference!)
            .Append(run.History[^1].EvidenceReference).Concat(policyEvidence)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();

        var authorizationRequest = new StaticValidationResultAuthorizationRequest(
            Guid.NewGuid(), request.ValidationId, request.GenerationId, request.Identity.TenantId,
            request.Identity.SubjectId, request.Purpose, request.ExpectedCandidateSha256Digest,
            reportDigest, request.RequiredControlIds.Order(StringComparer.Ordinal).ToImmutableArray(),
            reportEvidence, decision.DecidedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
        ValidateAuthorization(authorizationRequest, authorization);
        var allEvidence = reportEvidence.Concat(authorization.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new StaticValidationEvidenceRecord(
            request.ValidationId, request.GenerationId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose,
            request.ExpectedCandidateSha256Digest, decision.DecisionRequestId, report,
            reportDigest, allEvidence, authorization.DecidedAt);
        var evidenceReceipt = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidenceReceipt(record, evidenceReceipt);

        return new GovernedStaticValidationReceipt(
            request.ValidationId, request.GenerationId, request.DeliveryRunId,
            request.Identity.TenantId, decision.Outcome, ValidationGate.Static, report.IsAccepted,
            IsExecutable: false, CanAdvance: false, request.ExpectedCandidateSha256Digest,
            reportDigest, report.ControlReports, evidenceReceipt.EvidenceReference,
            allEvidence.Append(evidenceReceipt.EvidenceReference).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved Security Validation", evidenceReceipt.RecordedAt);
    }

    private static void ValidateDecision(
        StaticValidationPolicyInput input, GovernedIdentity identity,
        StaticValidationPolicyDecision decision)
    {
        if (decision.DecisionRequestId != input.DecisionRequestId || decision.ValidationId != input.ValidationId ||
            decision.GenerationId != input.GenerationId || decision.DeliveryRunId != input.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(decision.TenantId, input.TenantId) ||
            !StringComparer.Ordinal.Equals(decision.Environment, input.Environment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.CandidateSha256Digest, input.CandidateSha256Digest) ||
            !StringComparer.Ordinal.Equals(decision.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(decision.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Static Validation decision.");
        if (!decision.PolicySignatureValid || string.IsNullOrWhiteSpace(decision.PolicyVerificationEvidenceReference) ||
            decision.MaximumClassification > input.MaximumClassification || decision.MaximumClassification > identity.Clearance ||
            decision.Reasons.IsDefaultOrEmpty || decision.EvidenceReferences.IsDefaultOrEmpty || decision.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("Static Validation OPA signature, scope, reasons, or evidence is invalid.");
        if (decision.Outcome == GovernedIntentPolicyOutcome.Permit &&
            !decision.AllowedControlIds.SetEquals(input.RequiredControlIds))
            throw new UnauthorizedAccessException("OPA did not authorize the exact Static control set.");
    }

    private static void ValidateCandidate(
        GovernedStaticValidationRequest request, AuthorizedCodeGenerationCandidateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot.Receipt);
        ArgumentNullException.ThrowIfNull(snapshot.Candidate);
        snapshot.Candidate.Validate();
        var receipt = snapshot.Receipt;
        if (receipt.GenerationId != request.GenerationId || receipt.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(receipt.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            !StringComparer.Ordinal.Equals(receipt.GenerationEvidenceReference, request.ExpectedGenerationEvidenceReference))
            throw new InvalidOperationException("Code Generation candidate does not match the Static Validation request.");
        if (receipt.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !receipt.IsCodeCandidateReleased ||
            receipt.IsExecutable || receipt.IsApplied || receipt.CanAdvance || string.IsNullOrWhiteSpace(receipt.Content) ||
            string.IsNullOrWhiteSpace(receipt.GenerationEvidenceReference) || receipt.EvidenceReferences.IsDefaultOrEmpty ||
            !StringComparer.Ordinal.Equals(receipt.Content, snapshot.Candidate.Content) ||
            !receipt.GeneratedFilePaths.ToImmutableHashSet(StringComparer.Ordinal)
                .SetEquals(snapshot.Candidate.GeneratedFilePaths))
            throw new UnauthorizedAccessException("Code Generation candidate is not eligible for Static Validation.");
        foreach (var path in snapshot.Candidate.GeneratedFilePaths) GovernedGeneratedPath.Validate(path);
    }

    private static void ValidateRun(GovernedStaticValidationRequest request, SoftwareDeliveryRun run)
    {
        run.Validate();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.CodeGeneration || run.History.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Static Validation requires a matching run stopped at CodeGeneration.");
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.CodeGeneration).ToArray();
        if (run.History.Length != expected.Length || run.History.Where((item, index) =>
                item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) ||
                index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new InvalidOperationException("Delivery-run history is not a deterministic CodeGeneration boundary.");
    }

    private static ICodeValidationControl[] ValidateControls(
        GovernedStaticValidationRequest request, StaticValidationPolicyDecision decision,
        IEnumerable<ICodeValidationControl> controls)
    {
        var all = controls.ToArray();
        if (all.Any(item => string.IsNullOrWhiteSpace(item.ControlId)) ||
            all.GroupBy(item => item.ControlId, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new InvalidOperationException("Validation controls must have unique non-empty identities.");
        var selected = all.Where(item => item.Gate == ValidationGate.Static).ToArray();
        var ids = selected.Select(item => item.ControlId).ToImmutableHashSet(StringComparer.Ordinal);
        if (selected.Length == 0 || !ids.SetEquals(request.RequiredControlIds) ||
            !ids.SetEquals(decision.AllowedControlIds))
            throw new StaticValidationDependencyUnavailableException(
                "The exact policy-required Static validation controls are unavailable.");
        return selected;
    }

    private static void ValidateReport(GovernedStaticValidationRequest request, ValidationGateReport report)
    {
        if (report.Gate != ValidationGate.Static || !report.IsAccepted || report.ControlReports.IsDefaultOrEmpty ||
            !report.ControlReports.Select(item => item.ControlId).ToImmutableHashSet(StringComparer.Ordinal)
                .SetEquals(request.RequiredControlIds) || report.ControlReports.Any(item =>
                item.Gate != ValidationGate.Static || !item.Completed || !item.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) || item.Findings.IsDefault ||
                item.Findings.Any(finding => string.IsNullOrWhiteSpace(finding.RuleId) ||
                    !Enum.IsDefined(finding.Severity) || string.IsNullOrWhiteSpace(finding.Message) ||
                    string.IsNullOrWhiteSpace(finding.Location) || string.IsNullOrWhiteSpace(finding.EvidenceReference))))
            throw new UnauthorizedAccessException("Static Validation report is incomplete, blocking, or invalid.");
    }

    private static string Digest(string candidateDigest, ValidationGateReport report)
    {
        var canonical = string.Join('|', report.ControlReports.OrderBy(item => item.ControlId, StringComparer.Ordinal)
            .Select(item => $"{item.ControlId}:{item.Gate}:{item.Completed}:{item.EvidenceReference}:" +
                string.Join(',', item.Findings.OrderBy(finding => finding.RuleId, StringComparer.Ordinal)
                    .Select(finding => $"{finding.RuleId}:{finding.Severity}:{finding.Location}:{finding.Message}:{finding.EvidenceReference}"))));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{candidateDigest}|Static|{canonical}")))
            .ToLowerInvariant();
    }

    private static void ValidateAuthorization(
        StaticValidationResultAuthorizationRequest request,
        StaticValidationResultAuthorizationDecision decision)
    {
        if (decision.AuthorizationRequestId != request.AuthorizationRequestId ||
            decision.ValidationId != request.ValidationId || decision.GenerationId != request.GenerationId ||
            !StringComparer.Ordinal.Equals(decision.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.CandidateSha256Digest, request.CandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.ReportSha256Digest, request.ReportSha256Digest) ||
            !decision.IsAllowed || string.IsNullOrWhiteSpace(decision.Code) ||
            decision.EvidenceReferences.IsDefaultOrEmpty || decision.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Static Validation result authorization denied or mismatched.");
    }

    private static void ValidateEvidenceReceipt(
        StaticValidationEvidenceRecord record, StaticValidationEvidenceReceipt receipt)
    {
        if (receipt.ValidationId != record.ValidationId || receipt.GenerationId != record.GenerationId ||
            receipt.DeliveryRunId != record.DeliveryRunId || !StringComparer.Ordinal.Equals(receipt.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.CandidateSha256Digest, record.CandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.ReportSha256Digest, record.ReportSha256Digest) ||
            string.IsNullOrWhiteSpace(receipt.EvidenceReference) || receipt.RecordedAt < record.ValidatedAt)
            throw new InvalidOperationException("Static Validation evidence recorder returned a mismatched receipt.");
    }
}
