using System.Collections.Immutable;
using Platform.Governance.Evidence;
using Platform.Governance.Policies;

namespace Platform.Governance.GovernedActions;

public sealed class GovernedActionGateway(
    IPolicyBundleVerifier policyBundleVerifier,
    IOpaPolicyDecisionPoint policyDecisionPoint,
    IGovernanceEvidenceJournal evidenceJournal,
    IGovernedActionExecutor actionExecutor)
{
    public async Task<GovernedActionOutcome> ExecuteAsync(
        GovernedActionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        await RecordAsync(request, GovernanceEvidenceStage.Request, request.RequestedBySubjectId,
            "governed_action_requested", request.EvidenceReferences, request.RequestedAt, cancellationToken);

        var verification = await policyBundleVerifier.VerifyAsync(request.PolicyBundle, cancellationToken);
        ValidateVerification(request, verification);
        await RecordAsync(request, GovernanceEvidenceStage.PolicyVerification, request.RequestedBySubjectId,
            "signed_policy_bundle_verified", [verification.VerificationEvidenceReference],
            verification.VerifiedAt, cancellationToken);

        var decisionRequestId = Guid.NewGuid();
        var decision = await policyDecisionPoint.EvaluateAsync(
            new OpaPolicyInput(decisionRequestId, request, verification, verification.VerifiedAt),
            cancellationToken);
        ValidateDecision(request, verification, decisionRequestId, decision);
        await RecordAsync(request, GovernanceEvidenceStage.PolicyDecision, request.RequestedBySubjectId,
            $"opa_{decision.Outcome.ToString().ToLowerInvariant()}", decision.EvidenceReferences,
            decision.DecidedAt, cancellationToken);

        var decisionEvidenceReference = decision.EvidenceReferences[0];
        if (decision.Outcome != OpaDecisionOutcome.Permit)
        {
            await RecordAsync(request, GovernanceEvidenceStage.Denial, request.RequestedBySubjectId,
                "action_denied_fail_closed", decision.EvidenceReferences, decision.DecidedAt, cancellationToken);
            return new GovernedActionOutcome(request.RequestId, decision.Outcome, false, null, decisionEvidenceReference);
        }

        await RecordAsync(request, GovernanceEvidenceStage.Approval, request.ApprovedBySubjectId,
            "separation_of_duties_approval", [request.ApprovalEvidenceReference],
            decision.DecidedAt, cancellationToken);

        var idempotencyKey = $"governed-action:{request.RequestId:D}:{request.ActionName}";
        var commandEvidence = request.EvidenceReferences
            .Add(request.ApprovalEvidenceReference)
            .Add(verification.VerificationEvidenceReference)
            .AddRange(decision.EvidenceReferences)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var command = new AuthorizedActionCommand(
            request.RequestId, decisionRequestId, request.TenantId, request.RequestedBySubjectId,
            request.Environment, request.Classification, request.ActionName, request.TargetResource,
            request.Parameters, request.PolicyBundle.BundleId, request.PolicyBundle.Version,
            request.PolicyBundle.Sha256Digest, request.ApprovalEvidenceReference, idempotencyKey, commandEvidence);

        await RecordAsync(request, GovernanceEvidenceStage.ActionIntent, request.RequestedBySubjectId,
            idempotencyKey, commandEvidence, decision.DecidedAt, cancellationToken);
        var result = await actionExecutor.ExecuteAsync(command, cancellationToken);
        ValidateResult(command, decision, result);
        await RecordAsync(request, GovernanceEvidenceStage.Result, request.RequestedBySubjectId,
            result.ResultReference, result.EvidenceReferences, result.CompletedAt, cancellationToken);
        return new GovernedActionOutcome(request.RequestId, decision.Outcome, true, result, decisionEvidenceReference);
    }

    private static void ValidateVerification(GovernedActionRequest request, PolicyBundleVerification verification)
    {
        ArgumentNullException.ThrowIfNull(verification);
        if (!verification.SignatureValid ||
            !StringComparer.Ordinal.Equals(verification.BundleId, request.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(verification.Version, request.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(verification.Sha256Digest, request.PolicyBundle.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(verification.Environment, request.Environment))
            throw new UnauthorizedAccessException("Policy bundle verification failed closed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(verification.VerificationEvidenceReference);
        if (verification.VerifiedAt < request.PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Policy verification predates bundle activation.");
    }

    private static void ValidateDecision(
        GovernedActionRequest request,
        PolicyBundleVerification verification,
        Guid decisionRequestId,
        OpaPolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.DecisionRequestId != decisionRequestId ||
            !StringComparer.Ordinal.Equals(decision.BundleId, verification.BundleId) ||
            !StringComparer.Ordinal.Equals(decision.BundleVersion, verification.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.BundleSha256Digest, verification.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(decision.Environment, request.Environment) ||
            !Enum.IsDefined<OpaDecisionOutcome>(decision.Outcome))
            throw new UnauthorizedAccessException("OPA returned a mismatched decision; action denied fail closed.");
        if (decision.Reasons.IsDefaultOrEmpty || decision.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("OPA decisions require reasons and evidence.");
        foreach (var value in decision.Reasons) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var value in decision.EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (decision.DecidedAt < verification.VerifiedAt)
            throw new InvalidOperationException("OPA decision predates policy verification.");
    }

    private static void ValidateResult(
        AuthorizedActionCommand command,
        OpaPolicyDecision decision,
        GovernedActionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.RequestId != command.RequestId ||
            !StringComparer.Ordinal.Equals(result.IdempotencyKey, command.IdempotencyKey))
            throw new InvalidOperationException("Action executor returned a mismatched result.");
        ArgumentException.ThrowIfNullOrWhiteSpace(result.ResultReference);
        if (result.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Action results require evidence.");
        foreach (var value in result.EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (result.CompletedAt < decision.DecidedAt)
            throw new InvalidOperationException("Action result predates its policy decision.");
    }

    private Task RecordAsync(
        GovernedActionRequest request,
        GovernanceEvidenceStage stage,
        string actor,
        string detail,
        ImmutableArray<string> evidence,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        evidenceJournal.AppendAsync(
            new GovernanceEvidenceRecord(request.RequestId, request.TenantId, stage, actor, detail, evidence, occurredAt),
            cancellationToken);
}
