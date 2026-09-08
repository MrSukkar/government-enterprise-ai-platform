using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.Validation;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedSecurityValidationRequest(
    Guid ValidationId, Guid StaticValidationId, Guid GenerationId, Guid DeliveryRunId,
    string ExpectedCandidateSha256Digest, string ExpectedStaticReportSha256Digest,
    string ExpectedStaticEvidenceReference, ImmutableArray<string> RequiredControlIds,
    GovernedIdentity Identity, string Purpose, DataClassification MaximumClassification,
    string AuthorizationEvidenceReference, string Environment,
    IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedSecurityValidationRequest Validate()
    {
        if (ValidationId == Guid.Empty || StaticValidationId == Guid.Empty || GenerationId == Guid.Empty || DeliveryRunId == Guid.Empty)
            throw new InvalidOperationException("Security Validation and prerequisite identities are required.");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedCandidateSha256Digest, "code candidate");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedStaticReportSha256Digest, "Static Validation report");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedStaticEvidenceReference);
        if (RequiredControlIds.IsDefaultOrEmpty || RequiredControlIds.Any(string.IsNullOrWhiteSpace) ||
            RequiredControlIds.Distinct(StringComparer.Ordinal).Count() != RequiredControlIds.Length)
            throw new InvalidOperationException("Unique required Security control identities are required.");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("developer.internal-service.security-validation.create"))
            throw new UnauthorizedAccessException("Governed Security Validation permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Security Validation.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentNullException.ThrowIfNull(PolicyBundle);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Security Validation policy environment or time is invalid.");
        return this;
    }
}

public interface IAuthorizedStaticValidationReceiptReader
{
    Task<GovernedStaticValidationReceipt?> LoadAsync(
        Guid validationId, string tenantId, string purpose, Guid generationId, Guid deliveryRunId,
        string candidateSha256Digest, string staticReportSha256Digest,
        string staticEvidenceReference, CancellationToken cancellationToken);
}

public interface ISecurityValidationCodeGenerationCandidateReader
{
    Task<AuthorizedCodeGenerationCandidateSnapshot?> LoadAsync(
        Guid generationId, string tenantId, string purpose, string candidateSha256Digest,
        Guid staticValidationId, string staticEvidenceReference, CancellationToken cancellationToken);
}

public interface ISecurityValidationDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, string purpose,
        Guid generationId, Guid staticValidationId, string candidateSha256Digest,
        string staticReportSha256Digest, CancellationToken cancellationToken);
}

public sealed record SecurityValidationPolicyInput(
    Guid DecisionRequestId, Guid ValidationId, Guid StaticValidationId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, string CandidateSha256Digest,
    string StaticReportSha256Digest, string StaticEvidenceReference,
    ImmutableArray<string> RequiredControlIds, IntentPolicyBundleReference PolicyBundle,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset EvaluatedAt);

public sealed record SecurityValidationPolicyDecision(
    Guid DecisionRequestId, Guid ValidationId, Guid StaticValidationId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, string Environment, string CandidateSha256Digest, string StaticReportSha256Digest,
    string BundleId, string BundleVersion, string BundleSha256Digest,
    bool PolicySignatureValid, string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome, DataClassification MaximumClassification,
    ImmutableHashSet<string> AllowedControlIds, ImmutableHashSet<string> RequiredRoles,
    string OutputKind, ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ISecurityValidationPolicyGate
{
    Task<SecurityValidationPolicyDecision> EvaluateAsync(
        SecurityValidationPolicyInput input, CancellationToken cancellationToken);
}

public sealed record SecurityValidationResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid ValidationId, Guid GenerationId, string TenantId,
    string SubjectId, string Purpose, GovernedIdentity Identity,
    DataClassification MaximumClassification, string Environment,
    string CandidateSha256Digest, string StaticReportSha256Digest, string SecurityReportSha256Digest,
    ImmutableHashSet<string> RequiredRoles, ImmutableArray<string> ControlIds,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record SecurityValidationResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid ValidationId, Guid GenerationId, string TenantId,
    string SecurityReportSha256Digest, bool IsAllowed, string Code,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface ISecurityValidationResultAuthorizer
{
    Task<SecurityValidationResultAuthorizationDecision> AuthorizeAsync(
        SecurityValidationResultAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record SecurityValidationEvidenceRecord(
    Guid ValidationId, Guid StaticValidationId, Guid GenerationId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose,
    string CandidateSha256Digest, string StaticReportSha256Digest,
    Guid PolicyDecisionRequestId, ValidationGateReport Report, string SecurityReportSha256Digest,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset ValidatedAt);

public sealed record SecurityValidationEvidenceReceipt(
    Guid ValidationId, Guid GenerationId, Guid DeliveryRunId, string TenantId,
    string SecurityReportSha256Digest, string EvidenceReference, DateTimeOffset RecordedAt);

public interface ISecurityValidationEvidenceRecorder
{
    Task<SecurityValidationEvidenceReceipt> RecordAsync(
        SecurityValidationEvidenceRecord record, CancellationToken cancellationToken);
}

public sealed class SecurityValidationDependencyUnavailableException(string message) : Exception(message);

public sealed record GovernedSecurityValidationReceipt(
    Guid ValidationId, Guid StaticValidationId, Guid GenerationId, Guid DeliveryRunId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, ValidationGate Gate, bool IsAccepted,
    bool IsExecutable, bool CanAdvance, string CandidateSha256Digest, string StaticReportSha256Digest,
    string? SecurityReportSha256Digest, ImmutableArray<ValidationControlReport> ControlReports,
    string? ValidationEvidenceReference, ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedSecurityValidationEngine
{
    public async Task<GovernedSecurityValidationReceipt> ValidateAsync(
        GovernedSecurityValidationRequest request, ISecurityValidationPolicyGate policyGate,
        IAuthorizedStaticValidationReceiptReader staticReader,
        ISecurityValidationCodeGenerationCandidateReader candidateReader,
        ISecurityValidationDeliveryRunReader runReader, IEnumerable<ICodeValidationControl> controls,
        ISecurityValidationResultAuthorizer resultAuthorizer,
        ISecurityValidationEvidenceRecorder evidenceRecorder, CancellationToken cancellationToken)
    {
        request.Validate();
        var initialEvidence = ImmutableArray.Create(
            request.ExpectedStaticEvidenceReference, request.AuthorizationEvidenceReference);
        var input = new SecurityValidationPolicyInput(
            Guid.NewGuid(), request.ValidationId, request.StaticValidationId, request.GenerationId,
            request.DeliveryRunId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Purpose, request.Environment, request.MaximumClassification,
            request.ExpectedCandidateSha256Digest, request.ExpectedStaticReportSha256Digest,
            request.ExpectedStaticEvidenceReference,
            request.RequiredControlIds.Order(StringComparer.Ordinal).ToImmutableArray(),
            request.PolicyBundle, initialEvidence, request.RequestedAt);
        var decision = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidateDecision(input, request.Identity, decision);
        var policyEvidence = initialEvidence.Append(decision.PolicyVerificationEvidenceReference)
            .Concat(decision.EvidenceReferences).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        if (decision.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedSecurityValidationReceipt(
                request.ValidationId, request.StaticValidationId, request.GenerationId,
                request.DeliveryRunId, request.Identity.TenantId, decision.Outcome,
                ValidationGate.Security, false, false, false, request.ExpectedCandidateSha256Digest,
                request.ExpectedStaticReportSha256Digest, null, [], null, policyEvidence,
                "Policy denial requires a new governed Security Validation request", decision.DecidedAt);

        var staticReceipt = await staticReader.LoadAsync(request.StaticValidationId,
            request.Identity.TenantId, request.Purpose, request.GenerationId, request.DeliveryRunId,
            request.ExpectedCandidateSha256Digest, request.ExpectedStaticReportSha256Digest,
            request.ExpectedStaticEvidenceReference, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Static Validation receipt was not found.");
        ValidateStatic(request, staticReceipt);
        var snapshot = await candidateReader.LoadAsync(request.GenerationId,
            request.Identity.TenantId, request.Purpose, request.ExpectedCandidateSha256Digest,
            request.StaticValidationId, request.ExpectedStaticEvidenceReference, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Code Generation candidate was not found.");
        ValidateCandidate(request, snapshot);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId,
            request.Purpose, request.GenerationId, request.StaticValidationId,
            request.ExpectedCandidateSha256Digest, request.ExpectedStaticReportSha256Digest, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);
        var selected = ValidateControls(request, decision, controls);
        var report = await new CodeValidationPipeline(selected).ExecuteAsync(
            new CodeValidationRequest(run, snapshot.Candidate, ValidationGate.Security), cancellationToken);
        ValidateReport(request, report);
        var reportDigest = Digest(request.ExpectedCandidateSha256Digest, request.ExpectedStaticReportSha256Digest, report);
        var reportEvidence = report.ControlReports.Select(item => item.EvidenceReference)
            .Concat(report.ControlReports.SelectMany(item => item.Findings).Select(item => item.EvidenceReference))
            .Concat(staticReceipt.EvidenceReferences).Append(staticReceipt.ValidationEvidenceReference!)
            .Concat(snapshot.Receipt.EvidenceReferences).Append(snapshot.Receipt.GenerationEvidenceReference!)
            .Append(run.History[^1].EvidenceReference).Concat(policyEvidence)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var authRequest = new SecurityValidationResultAuthorizationRequest(
            Guid.NewGuid(), request.ValidationId, request.GenerationId, request.Identity.TenantId,
            request.Identity.SubjectId, request.Purpose, request.Identity,
            decision.MaximumClassification, request.Environment,
            request.ExpectedCandidateSha256Digest, request.ExpectedStaticReportSha256Digest,
            reportDigest, decision.RequiredRoles,
            request.RequiredControlIds.Order(StringComparer.Ordinal).ToImmutableArray(),
            reportEvidence, decision.DecidedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authRequest, cancellationToken);
        ValidateAuthorization(authRequest, authorization);
        var allEvidence = reportEvidence.Concat(authorization.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new SecurityValidationEvidenceRecord(
            request.ValidationId, request.StaticValidationId, request.GenerationId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose,
            request.ExpectedCandidateSha256Digest,
            request.ExpectedStaticReportSha256Digest, decision.DecisionRequestId,
            report, reportDigest, allEvidence, authorization.DecidedAt);
        var evidence = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidence(record, evidence);
        return new GovernedSecurityValidationReceipt(
            request.ValidationId, request.StaticValidationId, request.GenerationId,
            request.DeliveryRunId, request.Identity.TenantId, decision.Outcome,
            ValidationGate.Security, true, IsExecutable: false, CanAdvance: false,
            request.ExpectedCandidateSha256Digest, request.ExpectedStaticReportSha256Digest,
            reportDigest, report.ControlReports, evidence.EvidenceReference,
            allEvidence.Append(evidence.EvidenceReference).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved Sandbox", evidence.RecordedAt);
    }

    private static void ValidateDecision(SecurityValidationPolicyInput input, GovernedIdentity identity, SecurityValidationPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.ValidationId != input.ValidationId ||
            value.StaticValidationId != input.StaticValidationId || value.GenerationId != input.GenerationId ||
            value.DeliveryRunId != input.DeliveryRunId || !StringComparer.Ordinal.Equals(value.TenantId, input.TenantId) ||
            !StringComparer.Ordinal.Equals(value.Environment, input.Environment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, input.CandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.StaticReportSha256Digest, input.StaticReportSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(value.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Security Validation decision.");
        if (!value.PolicySignatureValid || string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) ||
            value.MaximumClassification > input.MaximumClassification || value.MaximumClassification > identity.Clearance ||
            value.Reasons.IsDefaultOrEmpty || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("Security Validation OPA decision is invalid.");
        if (value.Outcome == GovernedIntentPolicyOutcome.Permit &&
            (!value.AllowedControlIds.SetEquals(input.RequiredControlIds) || value.RequiredRoles.IsEmpty ||
             !value.RequiredRoles.IsSubsetOf(identity.Roles) ||
             !StringComparer.Ordinal.Equals(value.OutputKind, "security-report")))
            throw new UnauthorizedAccessException("OPA did not authorize the exact Security control set.");
    }

    private static void ValidateStatic(GovernedSecurityValidationRequest request, GovernedStaticValidationReceipt value)
    {
        if (value.ValidationId != request.StaticValidationId || value.GenerationId != request.GenerationId ||
            value.DeliveryRunId != request.DeliveryRunId || !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ReportSha256Digest, request.ExpectedStaticReportSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.ValidationEvidenceReference, request.ExpectedStaticEvidenceReference) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || value.Gate != ValidationGate.Static ||
            !value.IsAccepted || value.IsExecutable || value.CanAdvance || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Static Validation prerequisite is invalid or mismatched.");
    }

    private static void ValidateCandidate(GovernedSecurityValidationRequest request, AuthorizedCodeGenerationCandidateSnapshot value)
    {
        value.Candidate.Validate();
        if (value.Receipt.GenerationId != request.GenerationId || value.Receipt.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.Receipt.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.Receipt.CandidateSha256Digest, request.ExpectedCandidateSha256Digest) ||
            value.Receipt.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.Receipt.IsCodeCandidateReleased ||
            value.Receipt.IsExecutable || value.Receipt.IsApplied || value.Receipt.CanAdvance ||
            !StringComparer.Ordinal.Equals(value.Receipt.Content, value.Candidate.Content) ||
            !value.Receipt.GeneratedFilePaths.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(value.Candidate.GeneratedFilePaths))
            throw new UnauthorizedAccessException("Code candidate is invalid or mismatched.");
    }

    private static void ValidateRun(GovernedSecurityValidationRequest request, SoftwareDeliveryRun run)
    {
        run.Validate();
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.StaticValidation).ToArray();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.StaticValidation || run.History.Length != expected.Length ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) || index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run is not stopped at a valid StaticValidation boundary.");
    }

    private static ICodeValidationControl[] ValidateControls(GovernedSecurityValidationRequest request, SecurityValidationPolicyDecision decision, IEnumerable<ICodeValidationControl> controls)
    {
        var all = controls.ToArray();
        if (all.Any(item => string.IsNullOrWhiteSpace(item.ControlId)) || all.GroupBy(item => item.ControlId, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new InvalidOperationException("Validation control identities must be unique.");
        var selected = all.Where(item => item.Gate == ValidationGate.Security).ToArray();
        var ids = selected.Select(item => item.ControlId).ToImmutableHashSet(StringComparer.Ordinal);
        if (selected.Length == 0 || !ids.SetEquals(request.RequiredControlIds) || !ids.SetEquals(decision.AllowedControlIds))
            throw new SecurityValidationDependencyUnavailableException("The exact required Security controls are unavailable.");
        return selected;
    }

    private static void ValidateReport(GovernedSecurityValidationRequest request, ValidationGateReport report)
    {
        if (report.Gate != ValidationGate.Security || !report.IsAccepted || report.ControlReports.IsDefaultOrEmpty ||
            !report.ControlReports.Select(item => item.ControlId).ToImmutableHashSet(StringComparer.Ordinal).SetEquals(request.RequiredControlIds) ||
            report.ControlReports.Any(item => item.Gate != ValidationGate.Security || !item.Completed || !item.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) || item.Findings.IsDefault || item.Findings.Any(finding =>
                    string.IsNullOrWhiteSpace(finding.RuleId) || !Enum.IsDefined(finding.Severity) ||
                    string.IsNullOrWhiteSpace(finding.Message) || string.IsNullOrWhiteSpace(finding.Location) ||
                    string.IsNullOrWhiteSpace(finding.EvidenceReference))))
            throw new UnauthorizedAccessException("Security Validation report is incomplete, blocking, or invalid.");
    }

    private static string Digest(string candidate, string staticReport, ValidationGateReport report)
    {
        var canonical = string.Join('|', report.ControlReports.OrderBy(item => item.ControlId, StringComparer.Ordinal)
            .Select(item => $"{item.ControlId}:{item.Completed}:{item.EvidenceReference}:" + string.Join(',',
                item.Findings.OrderBy(finding => finding.RuleId, StringComparer.Ordinal).Select(finding =>
                    $"{finding.RuleId}:{finding.Severity}:{finding.Location}:{finding.Message}:{finding.EvidenceReference}"))));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{candidate}|{staticReport}|Security|{canonical}"))).ToLowerInvariant();
    }

    private static void ValidateAuthorization(SecurityValidationResultAuthorizationRequest request, SecurityValidationResultAuthorizationDecision value)
    {
        if (value.AuthorizationRequestId != request.AuthorizationRequestId || value.ValidationId != request.ValidationId ||
            value.GenerationId != request.GenerationId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SecurityReportSha256Digest, request.SecurityReportSha256Digest) ||
            !value.IsAllowed || string.IsNullOrWhiteSpace(value.Code) || value.EvidenceReferences.IsDefaultOrEmpty ||
            value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Security Validation result authorization denied or mismatched.");
    }

    private static void ValidateEvidence(SecurityValidationEvidenceRecord record, SecurityValidationEvidenceReceipt value)
    {
        if (value.ValidationId != record.ValidationId || value.GenerationId != record.GenerationId ||
            value.DeliveryRunId != record.DeliveryRunId || !StringComparer.Ordinal.Equals(value.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SecurityReportSha256Digest, record.SecurityReportSha256Digest) ||
            string.IsNullOrWhiteSpace(value.EvidenceReference) || value.RecordedAt < record.ValidatedAt)
            throw new InvalidOperationException("Security Validation evidence receipt is invalid.");
    }
}
