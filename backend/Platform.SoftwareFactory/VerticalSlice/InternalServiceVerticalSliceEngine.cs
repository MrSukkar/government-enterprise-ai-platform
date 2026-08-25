using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.VerticalSlice;

public sealed class InternalServiceVerticalSliceEngine(
    IVerticalSliceStageExecutor stageExecutor,
    IVerticalSliceRunStore runStore)
{
    private static readonly DeliveryStage[] ApprovedSequence = Enum.GetValues<DeliveryStage>();

    public async Task<VerticalSliceRun> StartAsync(
        InternalServiceVerticalSliceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var idempotencyKey = Key(request.RunId, DeliveryStage.Intent, 0);
        var digestInput = string.Join('|', request.TenantId, request.DeveloperSubjectId,
            request.ServiceName, request.Intent, request.ExistingArchitectureReference);
        var receipt = new VerticalSliceStageReceipt(
            request.RunId, DeliveryStage.Intent, request.DeveloperSubjectId, idempotencyKey,
            $"intent:{request.RunId:D}", Sha256(digestInput), false, false, false, false, false,
            false, request.IntentEvidenceReferences, request.RequestedAt);
        ValidateReceipt(request, [], receipt, DeliveryStage.Intent, idempotencyKey);
        var run = new VerticalSliceRun(request, 1, [receipt], receipt.CompletedAt);
        var persisted = await runStore.CreateAtomicallyAsync(run, cancellationToken);
        return ValidatePersisted(run, persisted);
    }

    public async Task<VerticalSliceRun> AdvanceAsync(
        Guid runId,
        string tenantId,
        CancellationToken cancellationToken)
    {
        var current = await runStore.LoadAsync(runId, tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Vertical slice run was not found.");
        current.Validate();
        if (current.IsComplete) throw new InvalidOperationException("Vertical slice run is already complete.");
        var ordinal = current.Receipts.Length;
        var stage = ApprovedSequence[ordinal];
        var idempotencyKey = Key(runId, stage, ordinal);
        var context = new VerticalSliceExecutionContext(
            current.Request, stage, ordinal, current.Version, idempotencyKey, current.Receipts);
        var receipt = await stageExecutor.ExecuteAsync(context, cancellationToken);
        ValidateReceipt(current.Request, current.Receipts, receipt, stage, idempotencyKey);
        var advanced = current with
        {
            Version = current.Version + 1,
            Receipts = current.Receipts.Add(receipt),
            UpdatedAt = receipt.CompletedAt
        };
        advanced.Validate();
        var persisted = await runStore.AppendAtomicallyAsync(advanced, current.Version, cancellationToken);
        return ValidatePersisted(advanced, persisted);
    }

    public async Task<VerticalSliceRun> RunToCompletionAsync(
        InternalServiceVerticalSliceRequest request,
        CancellationToken cancellationToken)
    {
        var run = await StartAsync(request, cancellationToken);
        while (!run.IsComplete)
            run = await AdvanceAsync(run.Request.RunId, run.Request.TenantId, cancellationToken);
        return run;
    }

    private static void ValidateReceipt(
        InternalServiceVerticalSliceRequest request,
        ImmutableArray<VerticalSliceStageReceipt> prior,
        VerticalSliceStageReceipt receipt,
        DeliveryStage expectedStage,
        string expectedIdempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (receipt.RunId != request.RunId || receipt.Stage != expectedStage ||
            !StringComparer.Ordinal.Equals(receipt.IdempotencyKey, expectedIdempotencyKey))
            throw new InvalidOperationException("Vertical slice executor returned a mismatched receipt.");
        ArgumentException.ThrowIfNullOrWhiteSpace(receipt.CompletedBySubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(receipt.OutputReference);
        if (receipt.OutputSha256Digest.Length != 64 || receipt.OutputSha256Digest.Any(character => !Uri.IsHexDigit(character)) ||
            receipt.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Vertical slice receipt requires digest and evidence.");
        foreach (var value in receipt.EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var previousTime = prior.IsDefaultOrEmpty ? request.RequestedAt : prior[^1].CompletedAt;
        if (receipt.CompletedAt < previousTime) throw new InvalidOperationException("Vertical slice stage time moved backward.");
        if (receipt.ExternalEffectOccurred && receipt.Stage != DeliveryStage.Deployment)
            throw new InvalidOperationException("Only the governed deployment stage may record an external effect.");

        var stageOrdinal = Array.IndexOf(ApprovedSequence, receipt.Stage);
        var approvedPackagesOrdinal = Array.IndexOf(ApprovedSequence, DeliveryStage.ApprovedPackages);
        var humanReviewOrdinal = Array.IndexOf(ApprovedSequence, DeliveryStage.HumanReview);
        var artifactOrdinal = Array.IndexOf(ApprovedSequence, DeliveryStage.Artifact);
        var telemetryOrdinal = Array.IndexOf(ApprovedSequence, DeliveryStage.OpenTelemetry);
        var registrationOrdinal = Array.IndexOf(ApprovedSequence, DeliveryStage.AutomaticRegistration);
        if (stageOrdinal >= approvedPackagesOrdinal && !receipt.PolicyGateSatisfied)
            throw new UnauthorizedAccessException("Vertical slice policy gate is required from approved packages onward.");
        if (stageOrdinal >= humanReviewOrdinal && !receipt.HumanApprovalSatisfied)
            throw new UnauthorizedAccessException("Vertical slice human approval is required before Git and deployment.");
        if (receipt.Stage == DeliveryStage.HumanReview &&
            StringComparer.Ordinal.Equals(receipt.CompletedBySubjectId, request.DeveloperSubjectId))
            throw new UnauthorizedAccessException("Vertical slice review requires separation of duties.");
        if (stageOrdinal >= artifactOrdinal && !receipt.SupplyChainVerified)
            throw new UnauthorizedAccessException("Verified supply chain is required before deployment.");
        if (stageOrdinal >= telemetryOrdinal && !receipt.TelemetryEmitted)
            throw new InvalidOperationException("OpenTelemetry evidence is required after deployment.");
        if (stageOrdinal >= registrationOrdinal && !receipt.EnterpriseModelRegistered)
            throw new InvalidOperationException("Automatic Enterprise Model registration is required.");
    }

    private static VerticalSliceRun ValidatePersisted(VerticalSliceRun expected, VerticalSliceRun persisted)
    {
        ArgumentNullException.ThrowIfNull(persisted);
        persisted.Validate();
        if (!EquivalentRequest(persisted.Request, expected.Request) || persisted.Version != expected.Version ||
            persisted.Receipts.Length != expected.Receipts.Length ||
            !persisted.Receipts.Zip(expected.Receipts).All(pair => EquivalentReceipt(pair.First, pair.Second)) ||
            persisted.UpdatedAt != expected.UpdatedAt)
            throw new InvalidOperationException("Vertical slice store changed governed state.");
        return persisted;
    }

    private static bool EquivalentRequest(
        InternalServiceVerticalSliceRequest left,
        InternalServiceVerticalSliceRequest right) =>
        left.RunId == right.RunId && StringComparer.Ordinal.Equals(left.TenantId, right.TenantId) &&
        StringComparer.Ordinal.Equals(left.DeveloperSubjectId, right.DeveloperSubjectId) &&
        left.Permissions.SetEquals(right.Permissions) && StringComparer.Ordinal.Equals(left.ServiceName, right.ServiceName) &&
        StringComparer.Ordinal.Equals(left.Intent, right.Intent) &&
        left.EnterpriseContextReferences.SequenceEqual(right.EnterpriseContextReferences, StringComparer.Ordinal) &&
        left.ExistingSystemReferences.SequenceEqual(right.ExistingSystemReferences, StringComparer.Ordinal) &&
        StringComparer.Ordinal.Equals(left.ExistingArchitectureReference, right.ExistingArchitectureReference) &&
        left.ApprovedPackageReferences.SequenceEqual(right.ApprovedPackageReferences, StringComparer.Ordinal) &&
        left.IntentEvidenceReferences.SequenceEqual(right.IntentEvidenceReferences, StringComparer.Ordinal) &&
        left.RequestedAt == right.RequestedAt;

    private static bool EquivalentReceipt(VerticalSliceStageReceipt left, VerticalSliceStageReceipt right) =>
        left.RunId == right.RunId && left.Stage == right.Stage &&
        StringComparer.Ordinal.Equals(left.CompletedBySubjectId, right.CompletedBySubjectId) &&
        StringComparer.Ordinal.Equals(left.IdempotencyKey, right.IdempotencyKey) &&
        StringComparer.Ordinal.Equals(left.OutputReference, right.OutputReference) &&
        StringComparer.OrdinalIgnoreCase.Equals(left.OutputSha256Digest, right.OutputSha256Digest) &&
        left.PolicyGateSatisfied == right.PolicyGateSatisfied &&
        left.HumanApprovalSatisfied == right.HumanApprovalSatisfied &&
        left.SupplyChainVerified == right.SupplyChainVerified && left.TelemetryEmitted == right.TelemetryEmitted &&
        left.EnterpriseModelRegistered == right.EnterpriseModelRegistered &&
        left.ExternalEffectOccurred == right.ExternalEffectOccurred &&
        left.EvidenceReferences.ToHashSet(StringComparer.Ordinal).SetEquals(right.EvidenceReferences) &&
        left.CompletedAt == right.CompletedAt;

    private static string Key(Guid runId, DeliveryStage stage, int ordinal) =>
        $"vertical-slice:{runId:D}:{ordinal}:{stage}";

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
