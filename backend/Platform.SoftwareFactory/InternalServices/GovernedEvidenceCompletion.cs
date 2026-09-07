using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.Evidence.Chain;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedEvidenceCompletionRequest(
    Guid CompletionId, Guid ContextualizationId, Guid DeliveryRunId, Guid ChainId,
    string CorrelationId, string ExpectedContextualizationEvidenceReference,
    string PayloadSha256Digest, ImmutableArray<string> TraceReferences,
    GovernedIdentity Identity, string Purpose, DataClassification MaximumClassification,
    string AuthorizationEvidenceReference, string Environment,
    IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedEvidenceCompletionRequest Validate()
    {
        if (CompletionId == Guid.Empty || ContextualizationId == Guid.Empty || DeliveryRunId == Guid.Empty || ChainId == Guid.Empty)
            throw new InvalidOperationException("Evidence completion identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(CorrelationId); ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedContextualizationEvidenceReference);
        GovernedAiPlanningRequest.ValidateDigest(PayloadSha256Digest, "Evidence completion payload");
        if (TraceReferences.IsDefaultOrEmpty) throw new InvalidOperationException("Evidence completion trace references are required.");
        foreach (var value in TraceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("operator.internal-service.evidence.complete") ||
            !Identity.Permissions.Contains("evidence.append") || !Identity.Permissions.Contains("evidence.verify"))
            throw new UnauthorizedAccessException("Evidence completion, append, and verification permissions are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose); ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment); ArgumentNullException.ThrowIfNull(PolicyBundle); PolicyBundle.Validate();
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification ||
            !StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new UnauthorizedAccessException("Evidence completion scope is invalid.");
        return this;
    }
}

public interface IAuthorizedEnterpriseModelContextualizationReceiptReader
{ Task<GovernedEnterpriseModelContextualizationReceipt?> LoadAsync(Guid contextualizationId, string tenantId, CancellationToken cancellationToken); }
public interface IEvidenceCompletionDeliveryRunReader
{ Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken); }

public sealed record EvidenceCompletionPolicyInput(
    Guid DecisionRequestId, Guid CompletionId, Guid ContextualizationId, Guid DeliveryRunId,
    Guid ChainId, string CorrelationId, string ContextualizationEvidenceReference,
    string PayloadSha256Digest, ImmutableArray<string> TraceReferences,
    string TenantId, string SubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, IntentPolicyBundleReference PolicyBundle,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset EvaluatedAt);
public sealed record EvidenceCompletionPolicyDecision(
    Guid DecisionRequestId, Guid CompletionId, Guid ContextualizationId, Guid DeliveryRunId,
    Guid ChainId, string CorrelationId, string PayloadSha256Digest,
    string TenantId, string Environment, bool AppendFinalEvidenceAllowed,
    bool VerificationAllowed, bool WorkflowAdvancementAllowed,
    string BundleId, string BundleVersion, string BundleSha256Digest, bool PolicySignatureValid,
    string PolicyVerificationEvidenceReference, GovernedIntentPolicyOutcome Outcome,
    DataClassification MaximumClassification, ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);
public interface IEvidenceCompletionPolicyGate
{ Task<EvidenceCompletionPolicyDecision> EvaluateAsync(EvidenceCompletionPolicyInput input, CancellationToken cancellationToken); }

public sealed record EvidenceCompletionResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid CompletionId, string TenantId, string SubjectId,
    string Purpose, string Environment, DataClassification MaximumClassification,
    EvidenceEntry FinalEntry, EvidenceProofReport Proof,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);
public sealed record EvidenceCompletionResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid CompletionId, Guid ChainId, string TenantId,
    string HeadSha256Digest, bool IsAllowed, string Code,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);
public interface IEvidenceCompletionResultAuthorizer
{ Task<EvidenceCompletionResultAuthorizationDecision> AuthorizeAsync(EvidenceCompletionResultAuthorizationRequest request, CancellationToken cancellationToken); }

public sealed record GovernedEvidenceCompletionReceipt(
    Guid CompletionId, Guid ContextualizationId, Guid DeliveryRunId, Guid ChainId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, bool IsAccepted, bool EvidenceAppended,
    bool EvidenceCryptographicallyVerified, bool EvidenceCompleted, bool VerticalSliceComplete,
    bool WorkflowAdvanced, bool CanAdvance, string? RootSha256Digest, string? HeadSha256Digest,
    int VerifiedEntryCount, string? VerificationAuthorizationEvidenceReference,
    ImmutableArray<string> EvidenceReferences, string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedEvidenceCompletionEngine
{
    public async Task<GovernedEvidenceCompletionReceipt> CompleteAsync(
        GovernedEvidenceCompletionRequest request, IEvidenceCompletionPolicyGate policyGate,
        IAuthorizedEnterpriseModelContextualizationReceiptReader contextualizationReader,
        IEvidenceCompletionDeliveryRunReader runReader, IEvidenceChainStore store,
        IEvidenceAccessAuthorizer evidenceAccessAuthorizer, IEvidenceSigner signer,
        IEvidenceSignatureVerifier signatureVerifier, IEvidenceCompletionResultAuthorizer resultAuthorizer,
        CancellationToken cancellationToken)
    {
        request.Validate();
        var input = new EvidenceCompletionPolicyInput(Guid.NewGuid(), request.CompletionId, request.ContextualizationId,
            request.DeliveryRunId, request.ChainId, request.CorrelationId,
            request.ExpectedContextualizationEvidenceReference, request.PayloadSha256Digest,
            request.TraceReferences, request.Identity.TenantId, request.Identity.SubjectId, request.Purpose,
            request.Environment, request.MaximumClassification, request.PolicyBundle,
            [request.AuthorizationEvidenceReference, request.ExpectedContextualizationEvidenceReference], request.RequestedAt);
        var policy = await policyGate.EvaluateAsync(input, cancellationToken); ValidatePolicy(input, request.Identity, policy);
        var policyEvidence = Normalize(input.EvidenceReferences.Append(policy.PolicyVerificationEvidenceReference).Concat(policy.EvidenceReferences));
        if (policy.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new(request.CompletionId, request.ContextualizationId, request.DeliveryRunId, request.ChainId,
                request.Identity.TenantId, policy.Outcome, false, false, false, false, false, false, false,
                null, null, 0, null, policyEvidence, "Policy denial requires a new governed Evidence completion request", policy.DecidedAt);
        var contextualization = await contextualizationReader.LoadAsync(request.ContextualizationId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Enterprise Model contextualization receipt was not found.");
        ValidateContextualization(request, contextualization);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);
        var chainEngine = new CryptographicEvidenceEngine(store, evidenceAccessAuthorizer, signer, signatureVerifier);
        var traceReferences = request.TraceReferences.Append(request.ExpectedContextualizationEvidenceReference)
            .Append(run.History[^1].EvidenceReference).Concat(contextualization.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var append = new EvidenceAppendRequest(request.ChainId, request.Identity.TenantId, request.CorrelationId,
            EvidenceStage.Evidence, request.Identity.SubjectId, request.Identity.Permissions,
            request.MaximumClassification.ToString(), request.Purpose, request.PayloadSha256Digest,
            traceReferences, policy.DecidedAt);
        var finalEntry = await chainEngine.AppendAsync(append, cancellationToken);
        if (finalEntry.Stage != EvidenceStage.Evidence || finalEntry.Sequence != Enum.GetValues<EvidenceStage>().Length - 1 ||
            !Eq(finalEntry.ChainId, request.ChainId) || !Eq(finalEntry.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(finalEntry.PayloadSha256Digest, request.PayloadSha256Digest))
            throw new InvalidOperationException("Final Evidence entry is invalid or mismatched.");
        var verification = new EvidenceVerificationRequest(request.ChainId, request.Identity.TenantId,
            request.Identity.SubjectId, request.Identity.Permissions, request.Purpose,
            request.MaximumClassification.ToString(), finalEntry.Signature.SignedAt);
        var proof = await chainEngine.VerifyAsync(verification, cancellationToken); ValidateProof(request, finalEntry, proof);
        var resultEvidence = Normalize(policyEvidence.Concat(contextualization.EvidenceReferences)
            .Append(request.ExpectedContextualizationEvidenceReference).Append(finalEntry.AuthorizationEvidenceReference)
            .Append(finalEntry.EntrySha256Digest).Append(proof.AuthorizationEvidenceReference));
        var authorizationRequest = new EvidenceCompletionResultAuthorizationRequest(Guid.NewGuid(), request.CompletionId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, finalEntry, proof, resultEvidence, proof.VerifiedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken); ValidateAuthorization(authorizationRequest, authorization);
        return new(request.CompletionId, request.ContextualizationId, request.DeliveryRunId, request.ChainId,
            request.Identity.TenantId, policy.Outcome, true, true, true, true, true, false, false,
            proof.RootSha256Digest, proof.HeadSha256Digest, proof.EntryProofs.Length,
            proof.AuthorizationEvidenceReference, Normalize(resultEvidence.Concat(authorization.EvidenceReferences)),
            "Complete — approved Create Internal Service vertical slice proven", authorization.DecidedAt);
    }

    private static void ValidatePolicy(EvidenceCompletionPolicyInput input, GovernedIdentity identity, EvidenceCompletionPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.CompletionId != input.CompletionId || value.ContextualizationId != input.ContextualizationId ||
            value.DeliveryRunId != input.DeliveryRunId || value.ChainId != input.ChainId || !Eq(value.CorrelationId, input.CorrelationId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.PayloadSha256Digest, input.PayloadSha256Digest) || !Eq(value.TenantId, input.TenantId) ||
            !Eq(value.Environment, input.Environment) || !Eq(value.BundleId, input.PolicyBundle.BundleId) || !Eq(value.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest)) throw new InvalidOperationException("OPA returned a mismatched Evidence completion decision.");
        if (!value.PolicySignatureValid || value.WorkflowAdvancementAllowed || string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) ||
            value.MaximumClassification > input.MaximumClassification || value.MaximumClassification > identity.Clearance || value.Reasons.IsDefaultOrEmpty ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt ||
            value.Outcome == GovernedIntentPolicyOutcome.Permit && (!value.AppendFinalEvidenceAllowed || !value.VerificationAllowed))
            throw new UnauthorizedAccessException("Evidence completion OPA decision is invalid.");
    }
    private static void ValidateContextualization(GovernedEvidenceCompletionRequest request, GovernedEnterpriseModelContextualizationReceipt value)
    {
        if (value.ContextualizationId != request.ContextualizationId || value.DeliveryRunId != request.DeliveryRunId || !Eq(value.TenantId, request.Identity.TenantId) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted || !value.EnterpriseModelContextualized || value.EnterpriseModelMutated ||
            value.WorkflowAdvanced || value.EvidenceCompleted || value.CanAdvance || !Eq(value.ContextualizationEvidenceReference, request.ExpectedContextualizationEvidenceReference) ||
            value.EvidenceReferences.IsDefaultOrEmpty) throw new UnauthorizedAccessException("Enterprise Model contextualization prerequisite is invalid or mismatched.");
    }
    private static void ValidateRun(GovernedEvidenceCompletionRequest request, SoftwareDeliveryRun run)
    {
        run.Validate(); var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.EnterpriseModel).ToArray();
        if (run.Id != request.DeliveryRunId || !Eq(run.TenantId, request.Identity.TenantId) || run.CurrentStage != DeliveryStage.EnterpriseModel || run.History.Length != expected.Length ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed || string.IsNullOrWhiteSpace(item.EvidenceReference) || index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run is not stopped at a valid Enterprise Model boundary.");
    }
    private static void ValidateProof(GovernedEvidenceCompletionRequest request, EvidenceEntry finalEntry, EvidenceProofReport proof)
    {
        var stages = Enum.GetValues<EvidenceStage>();
        if (proof.ChainId != request.ChainId || !Eq(proof.TenantId, request.Identity.TenantId) || !proof.IsValid || !proof.IsComplete ||
            string.IsNullOrWhiteSpace(proof.RootSha256Digest) || !StringComparer.OrdinalIgnoreCase.Equals(proof.HeadSha256Digest, finalEntry.EntrySha256Digest) ||
            proof.EntryProofs.Length != stages.Length || proof.EntryProofs.Where((item, index) => item.Sequence != index || item.Stage != stages[index] || !item.HashValid || !item.SignatureValid).Any() ||
            !proof.Failures.IsEmpty || string.IsNullOrWhiteSpace(proof.AuthorizationEvidenceReference))
            throw new System.Security.Cryptography.CryptographicException("Complete Evidence chain proof is invalid.");
    }
    private static void ValidateAuthorization(EvidenceCompletionResultAuthorizationRequest request, EvidenceCompletionResultAuthorizationDecision value)
    {
        if (value.AuthorizationRequestId != request.AuthorizationRequestId || value.CompletionId != request.CompletionId || value.ChainId != request.Proof.ChainId ||
            !Eq(value.TenantId, request.TenantId) || !StringComparer.OrdinalIgnoreCase.Equals(value.HeadSha256Digest, request.Proof.HeadSha256Digest) ||
            !value.IsAllowed || string.IsNullOrWhiteSpace(value.Code) || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Evidence completion result authorization is invalid.");
    }
    private static ImmutableArray<string> Normalize(IEnumerable<string> values) => values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
    private static bool Eq(string? left, string? right) => StringComparer.Ordinal.Equals(left, right);
    private static bool Eq(Guid left, Guid right) => left == right;
}
