using System.Collections.Immutable;

namespace Platform.Modeling.Simulation;

public sealed class EnterpriseSimulationEngine(
    IDigitalTwinSnapshotProvider snapshotProvider,
    IScenarioSimulationRuntime runtime,
    ISimulationRunStore runStore,
    TimeProvider timeProvider)
{
    public async Task<SimulationResult> RunAsync(
        EnterpriseSimulationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var digitalTwin = await snapshotProvider.LoadAuthorizedIsolatedSnapshotAsync(request, cancellationToken);
        var startedAt = timeProvider.GetUtcNow();
        ValidateDigitalTwin(request, digitalTwin, startedAt);

        var isolation = new SimulationIsolationProfile();
        if (isolation.HasProductionCredentials || isolation.AllowsExternalEffects ||
            isolation.NetworkAccess != SimulationNetworkAccess.None || digitalTwin.IsProductionConnected)
            throw new InvalidOperationException("Simulation isolation failed closed.");

        var idempotencyKey = $"simulation:{request.RequestId:D}:{request.Scenario.ScenarioId:D}:{digitalTwin.ModelSha256Digest}";
        var startingEvidence = digitalTwin.EvidenceReferences
            .AddRange(digitalTwin.Baseline.AuthorizationEvidenceReferences)
            .AddRange(request.Scenario.RecoveryEvidenceReferences)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var started = new SimulationRunRecord(
            request.RequestId, request.Scenario.ScenarioId, digitalTwin.SnapshotId, request.TenantId,
            idempotencyKey, SimulationRunState.Started, request.Scenario.RecoveryPlanReference,
            null, startingEvidence, startedAt);
        var persistedStarted = await runStore.CreateAtomicallyAsync(started, cancellationToken);
        ValidatePersisted(started, persistedStarted);

        var context = new SimulationExecutionContext(request, digitalTwin, isolation, idempotencyKey, startedAt);
        var result = await runtime.RunAsync(context, cancellationToken);
        ValidateResult(context, result);
        var completedEvidence = startingEvidence
            .AddRange(result.EvidenceReferences)
            .AddRange(result.ProjectedImpacts.SelectMany(item => item.EvidenceReferences))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var completed = started with
        {
            State = SimulationRunState.Completed,
            ResultReference = result.RecoveryAssessmentReference,
            EvidenceReferences = completedEvidence,
            UpdatedAt = result.CompletedAt
        };
        var persistedCompleted = await runStore.CompleteAtomicallyAsync(
            completed, SimulationRunState.Started, cancellationToken);
        ValidatePersisted(completed, persistedCompleted);
        return result;
    }

    private static void ValidateDigitalTwin(
        EnterpriseSimulationRequest request,
        DigitalTwinSnapshot digitalTwin,
        DateTimeOffset startedAt)
    {
        ArgumentNullException.ThrowIfNull(digitalTwin);
        if (digitalTwin.SnapshotId == Guid.Empty || digitalTwin.RequestId != request.RequestId ||
            !StringComparer.Ordinal.Equals(digitalTwin.TenantId, request.TenantId) || digitalTwin.IsProductionConnected)
            throw new UnauthorizedAccessException("Digital twin does not match the authorized isolated request.");
        ArgumentException.ThrowIfNullOrWhiteSpace(digitalTwin.ModelVersion);
        if (digitalTwin.ModelSha256Digest.Length != 64 ||
            digitalTwin.ModelSha256Digest.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Digital twin requires a model SHA-256 digest.");
        if (digitalTwin.EvidenceReferences.IsDefaultOrEmpty || digitalTwin.CreatedAt < request.RequestedAt ||
            digitalTwin.CreatedAt > startedAt)
            throw new InvalidOperationException("Digital twin evidence or creation time is invalid.");
        ArgumentNullException.ThrowIfNull(digitalTwin.Baseline);
        var baseline = digitalTwin.Baseline;
        if (baseline.RequestId != request.RequestId ||
            !StringComparer.Ordinal.Equals(baseline.TenantId, request.TenantId) ||
            baseline.Objects.IsDefaultOrEmpty || baseline.AuthorizationEvidenceReferences.IsDefaultOrEmpty ||
            baseline.CapturedAt < request.RequestedAt || baseline.CapturedAt > digitalTwin.CreatedAt)
            throw new UnauthorizedAccessException("Digital twin baseline is not an authorized current snapshot.");
        if (baseline.Objects.Select(item => item.Id).Distinct().Count() != baseline.Objects.Length)
            throw new InvalidOperationException("Digital twin baseline contains duplicate objects.");
        foreach (var enterpriseObject in baseline.Objects)
        {
            enterpriseObject.Validate();
            if (!StringComparer.Ordinal.Equals(enterpriseObject.TenantId, request.TenantId) ||
                !request.AuthorizedObjectScope.Contains(enterpriseObject.Id) ||
                enterpriseObject.Classification > request.MaximumClassification)
                throw new UnauthorizedAccessException("Digital twin provider exceeded authorized scope.");
        }
        var baselineIds = baseline.Objects.Select(item => item.Id).ToHashSet();
        if (request.Scenario.Perturbations.Any(item => !baselineIds.Contains(item.TargetObjectId)))
            throw new UnauthorizedAccessException("Digital twin omitted a perturbation target.");
    }

    private static void ValidateResult(SimulationExecutionContext context, SimulationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.RequestId != context.Request.RequestId ||
            result.ScenarioId != context.Request.Scenario.ScenarioId ||
            result.SnapshotId != context.DigitalTwin.SnapshotId ||
            !StringComparer.Ordinal.Equals(result.IdempotencyKey, context.IdempotencyKey) ||
            result.IsExternallyEffecting || result.IsAuthoritativeDecision)
            throw new InvalidOperationException("Simulation runtime returned a mismatched or authoritative result.");
        if (result.ProjectedImpacts.IsDefault || result.Assumptions.IsDefaultOrEmpty ||
            result.EvidenceReferences.IsDefaultOrEmpty || result.CompletedAt < context.StartedAt)
            throw new InvalidOperationException("Simulation result is incomplete or has invalid time.");
        if (!result.Assumptions.SequenceEqual(context.Request.Scenario.Assumptions, StringComparer.Ordinal))
            throw new InvalidOperationException("Simulation runtime changed the governed assumptions.");
        ArgumentException.ThrowIfNullOrWhiteSpace(result.RecoveryAssessmentReference);
        foreach (var impact in result.ProjectedImpacts)
        {
            if (!context.Request.AuthorizedObjectScope.Contains(impact.ObjectId) || impact.Confidence is < 0 or > 1 ||
                impact.EvidenceReferences.IsDefaultOrEmpty)
                throw new UnauthorizedAccessException("Projected impact exceeded authorized scope or evidence bounds.");
            ArgumentException.ThrowIfNullOrWhiteSpace(impact.Projection);
        }
        foreach (var value in result.EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
    }

    private static void ValidatePersisted(SimulationRunRecord expected, SimulationRunRecord persisted)
    {
        ArgumentNullException.ThrowIfNull(persisted);
        if (persisted.RequestId != expected.RequestId || persisted.ScenarioId != expected.ScenarioId ||
            persisted.SnapshotId != expected.SnapshotId ||
            !StringComparer.Ordinal.Equals(persisted.TenantId, expected.TenantId) ||
            !StringComparer.Ordinal.Equals(persisted.IdempotencyKey, expected.IdempotencyKey) ||
            persisted.State != expected.State ||
            !StringComparer.Ordinal.Equals(persisted.RecoveryPlanReference, expected.RecoveryPlanReference) ||
            !StringComparer.Ordinal.Equals(persisted.ResultReference, expected.ResultReference) ||
            !persisted.EvidenceReferences.ToHashSet(StringComparer.Ordinal).SetEquals(expected.EvidenceReferences) ||
            persisted.UpdatedAt != expected.UpdatedAt)
            throw new InvalidOperationException("Simulation run store changed governed state.");
    }
}
