using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;
using Platform.EnterpriseModel.Registration;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedAutomaticRegistrationManifest(
    Guid ManifestId, string Version, string Sha256Digest, string TenantId, string Environment,
    Guid ActivationId, string RuntimeIdentity, string ServiceIdentity, string ServiceVersion,
    string ArtifactDigest, string RegistryReference, string EnterpriseObjectType, string OwnerId,
    DataClassification Classification, string DeploymentEvidenceReference,
    string SupplyChainEvidenceReference, string ObservabilityEvidenceReference,
    string HumanApprovalReference, ImmutableArray<string> PolicyReferences,
    ImmutableArray<string> PermittedActions, ImmutableArray<string> EvidenceReferences,
    ImmutableArray<AutomaticRegistrationRelationship> Relationships,
    string SignatureEvidenceReference, bool SignatureValid)
{
    public GovernedAutomaticRegistrationManifest Validate()
    {
        if (ManifestId == Guid.Empty || ActivationId == Guid.Empty) throw new InvalidOperationException("Registration manifest identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        GovernedAiPlanningRequest.ValidateDigest(Sha256Digest, "automatic registration manifest");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId); ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(RuntimeIdentity); ArgumentException.ThrowIfNullOrWhiteSpace(ServiceIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceVersion); GovernedAiPlanningRequest.ValidateDigest(ArtifactDigest, "registration Artifact");
        ArgumentException.ThrowIfNullOrWhiteSpace(RegistryReference); ArgumentException.ThrowIfNullOrWhiteSpace(EnterpriseObjectType);
        ArgumentException.ThrowIfNullOrWhiteSpace(OwnerId); ArgumentException.ThrowIfNullOrWhiteSpace(DeploymentEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SupplyChainEvidenceReference); ArgumentException.ThrowIfNullOrWhiteSpace(ObservabilityEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(HumanApprovalReference); ArgumentException.ThrowIfNullOrWhiteSpace(SignatureEvidenceReference);
        if (!Enum.IsDefined(Classification) || !SignatureValid || PolicyReferences.IsDefaultOrEmpty || PermittedActions.IsDefaultOrEmpty || EvidenceReferences.IsDefaultOrEmpty || Relationships.IsDefault)
            throw new UnauthorizedAccessException("Signed registration manifest controls and evidence are required.");
        foreach (var value in PolicyReferences.Concat(PermittedActions).Concat(EvidenceReferences)) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var relationship in Relationships) relationship.Validate();
        return this;
    }
}

public sealed record GovernedAutomaticRegistrationRequest(
    Guid RegistrationId, Guid ActivationId, Guid DeliveryRunId, Guid ManifestId,
    string ManifestVersion, string ExpectedManifestSha256Digest,
    string ExpectedRuntimeIdentity, string ExpectedServiceIdentity, string ExpectedServiceVersion,
    string ExpectedArtifactDigest, string ExpectedOpenTelemetryEvidenceReference,
    GovernedIdentity Identity, string Purpose, DataClassification MaximumClassification,
    string AuthorizationEvidenceReference, string Environment,
    IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedAutomaticRegistrationRequest Validate()
    {
        if (RegistrationId == Guid.Empty || ActivationId == Guid.Empty || DeliveryRunId == Guid.Empty || ManifestId == Guid.Empty)
            throw new InvalidOperationException("Registration and prerequisite identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(ManifestVersion); GovernedAiPlanningRequest.ValidateDigest(ExpectedManifestSha256Digest, "registration manifest");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedRuntimeIdentity); ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedServiceIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedServiceVersion); GovernedAiPlanningRequest.ValidateDigest(ExpectedArtifactDigest, "registration Artifact");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedOpenTelemetryEvidenceReference); ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("operator.internal-service.registration.execute"))
            throw new UnauthorizedAccessException("Governed automatic registration permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose); ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment); ArgumentNullException.ThrowIfNull(PolicyBundle);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification) throw new UnauthorizedAccessException("Registration clearance is insufficient.");
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Registration policy scope or time is invalid.");
        return this;
    }
}

public interface IAuthorizedOpenTelemetryActivationReceiptReader
{ Task<GovernedOpenTelemetryActivationReceipt?> LoadAsync(Guid activationId, string tenantId, CancellationToken cancellationToken); }
public interface IAutomaticRegistrationDeliveryRunReader
{ Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken); }
public interface IGovernedAutomaticRegistrationManifestReader
{ Task<GovernedAutomaticRegistrationManifest?> LoadAsync(Guid manifestId, string version, string tenantId, CancellationToken cancellationToken); }

public sealed record AutomaticRegistrationPolicyInput(
    Guid DecisionRequestId, Guid RegistrationId, Guid ActivationId, Guid DeliveryRunId,
    Guid ManifestId, string ManifestVersion, string ManifestSha256Digest,
    string TenantId, string SubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, string RuntimeIdentity, string ServiceIdentity,
    string ServiceVersion, string ArtifactDigest, string OpenTelemetryEvidenceReference,
    IntentPolicyBundleReference PolicyBundle, ImmutableArray<string> EvidenceReferences, DateTimeOffset EvaluatedAt);
public sealed record AutomaticRegistrationPolicyDecision(
    Guid DecisionRequestId, Guid RegistrationId, Guid ActivationId, Guid DeliveryRunId,
    Guid ManifestId, string ManifestVersion, string ManifestSha256Digest,
    string TenantId, string Environment, string RuntimeIdentity, string ServiceIdentity,
    string ServiceVersion, string ArtifactDigest, bool RegistrationAllowed,
    bool WorkflowAdvancementAllowed, string BundleId, string BundleVersion, string BundleSha256Digest,
    bool PolicySignatureValid, string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome, DataClassification MaximumClassification,
    ImmutableArray<string> Reasons, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);
public interface IAutomaticRegistrationPolicyGate
{ Task<AutomaticRegistrationPolicyDecision> EvaluateAsync(AutomaticRegistrationPolicyInput input, CancellationToken cancellationToken); }

public sealed record AutomaticRegistrationResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid RegistrationId, string TenantId, string SubjectId,
    string Purpose, string Environment, DataClassification MaximumClassification,
    AutomaticRegistrationCommit Commit, ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);
public sealed record AutomaticRegistrationResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid RegistrationId, string TenantId, string RequestFingerprint,
    bool IsAllowed, string Code, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);
public interface IAutomaticRegistrationResultAuthorizer
{ Task<AutomaticRegistrationResultAuthorizationDecision> AuthorizeAsync(AutomaticRegistrationResultAuthorizationRequest request, CancellationToken cancellationToken); }

public sealed record AutomaticRegistrationEvidenceRecord(
    Guid RegistrationId, Guid ActivationId, Guid DeliveryRunId, string TenantId,
    Guid PolicyDecisionRequestId, GovernedAutomaticRegistrationManifest Manifest,
    AutomaticRegistrationCommit Commit, ImmutableArray<string> EvidenceReferences, DateTimeOffset AuthorizedAt);
public sealed record AutomaticRegistrationEvidenceReceipt(
    Guid RegistrationId, Guid DeliveryRunId, string TenantId, string RequestFingerprint,
    string EvidenceReference, DateTimeOffset RecordedAt);
public interface IAutomaticRegistrationEvidenceRecorder
{ Task<AutomaticRegistrationEvidenceReceipt> RecordAsync(AutomaticRegistrationEvidenceRecord record, CancellationToken cancellationToken); }

public sealed record GovernedAutomaticRegistrationReceipt(
    Guid RegistrationId, Guid ActivationId, Guid DeliveryRunId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, bool IsAccepted, bool AutomaticRegistrationOccurred,
    bool EnterpriseModelObjectPersisted, bool WorkflowAdvanced, bool CanAdvance,
    string ServiceIdentity, string ServiceVersion, string RuntimeIdentity, string ArtifactDigest,
    string? RequestFingerprint, RegistrationDisposition? Disposition, EnterpriseObjectId? EnterpriseObjectId,
    string? RegistrationEvidenceReference, ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedAutomaticRegistrationEngine
{
    public async Task<GovernedAutomaticRegistrationReceipt> RegisterAsync(
        GovernedAutomaticRegistrationRequest request, IAutomaticRegistrationPolicyGate policyGate,
        IAuthorizedOpenTelemetryActivationReceiptReader activationReader, IAutomaticRegistrationDeliveryRunReader runReader,
        IGovernedAutomaticRegistrationManifestReader manifestReader, IAutomaticRegistrationRepository registrationRepository,
        IAutomaticRegistrationResultAuthorizer resultAuthorizer, IAutomaticRegistrationEvidenceRecorder evidenceRecorder,
        CancellationToken cancellationToken)
    {
        request.Validate();
        var input = new AutomaticRegistrationPolicyInput(Guid.NewGuid(), request.RegistrationId, request.ActivationId,
            request.DeliveryRunId, request.ManifestId, request.ManifestVersion, request.ExpectedManifestSha256Digest,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, request.ExpectedRuntimeIdentity, request.ExpectedServiceIdentity,
            request.ExpectedServiceVersion, request.ExpectedArtifactDigest, request.ExpectedOpenTelemetryEvidenceReference,
            request.PolicyBundle, [request.AuthorizationEvidenceReference, request.ExpectedOpenTelemetryEvidenceReference], request.RequestedAt);
        var policy = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidatePolicy(input, request.Identity, policy);
        var policyEvidence = Normalize(input.EvidenceReferences.Append(policy.PolicyVerificationEvidenceReference).Concat(policy.EvidenceReferences));
        if (policy.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new(request.RegistrationId, request.ActivationId, request.DeliveryRunId, request.Identity.TenantId,
                policy.Outcome, false, false, false, false, false, request.ExpectedServiceIdentity,
                request.ExpectedServiceVersion, request.ExpectedRuntimeIdentity, request.ExpectedArtifactDigest,
                null, null, null, null, policyEvidence, "Policy denial requires a new governed Automatic Registration request", policy.DecidedAt);

        var activation = await activationReader.LoadAsync(request.ActivationId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed OpenTelemetry receipt was not found.");
        ValidateActivation(request, activation);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);
        var manifest = await manifestReader.LoadAsync(request.ManifestId, request.ManifestVersion, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed registration manifest was not found.");
        ValidateManifest(request, activation, manifest);
        var evidence = Normalize(policyEvidence.Concat(activation.EvidenceReferences).Append(activation.OpenTelemetryEvidenceReference!)
            .Append(run.History[^1].EvidenceReference).Concat(manifest.EvidenceReferences).Append(manifest.SignatureEvidenceReference));
        var registrationRequest = new AutomaticRegistrationRequest(request.RegistrationId,
            new AutomaticRegistrationKey(manifest.TenantId, manifest.Environment, manifest.ServiceIdentity),
            manifest.EnterpriseObjectType, manifest.OwnerId, manifest.Classification, $"sha256:{manifest.ArtifactDigest.ToLowerInvariant()}",
            manifest.RegistryReference, manifest.DeploymentEvidenceReference, manifest.SupplyChainEvidenceReference,
            manifest.ObservabilityEvidenceReference, manifest.HumanApprovalReference, manifest.PolicyReferences,
            manifest.PermittedActions, evidence, manifest.Relationships, policy.DecidedAt);
        var commit = await new AutomaticRegistrationEngine(registrationRepository).RegisterAsync(registrationRequest, cancellationToken);
        var resultEvidence = Normalize(evidence.Append(commit.EvidenceReference));
        var authorizationRequest = new AutomaticRegistrationResultAuthorizationRequest(Guid.NewGuid(), request.RegistrationId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, commit, resultEvidence, commit.CommittedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
        ValidateAuthorization(authorizationRequest, authorization);
        var allEvidence = Normalize(resultEvidence.Concat(authorization.EvidenceReferences));
        var record = new AutomaticRegistrationEvidenceRecord(request.RegistrationId, request.ActivationId,
            request.DeliveryRunId, request.Identity.TenantId, policy.DecisionRequestId, manifest, commit,
            allEvidence, authorization.DecidedAt);
        var evidenceReceipt = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidence(record, evidenceReceipt);
        return new(request.RegistrationId, request.ActivationId, request.DeliveryRunId, request.Identity.TenantId,
            policy.Outcome, true, true, true, false, false, manifest.ServiceIdentity, manifest.ServiceVersion,
            manifest.RuntimeIdentity, manifest.ArtifactDigest, commit.RequestFingerprint, commit.Disposition,
            commit.EnterpriseObject.Id, evidenceReceipt.EvidenceReference,
            Normalize(allEvidence.Append(evidenceReceipt.EvidenceReference)),
            "Separately approved Enterprise Model contextualization", evidenceReceipt.RecordedAt);
    }

    private static void ValidatePolicy(AutomaticRegistrationPolicyInput input, GovernedIdentity identity, AutomaticRegistrationPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.RegistrationId != input.RegistrationId || value.ActivationId != input.ActivationId ||
            value.DeliveryRunId != input.DeliveryRunId || value.ManifestId != input.ManifestId || !Eq(value.ManifestVersion, input.ManifestVersion) ||
            !EqI(value.ManifestSha256Digest, input.ManifestSha256Digest) || !Eq(value.TenantId, input.TenantId) || !Eq(value.Environment, input.Environment) ||
            !Eq(value.RuntimeIdentity, input.RuntimeIdentity) || !Eq(value.ServiceIdentity, input.ServiceIdentity) || !Eq(value.ServiceVersion, input.ServiceVersion) ||
            !EqI(value.ArtifactDigest, input.ArtifactDigest) || !Eq(value.BundleId, input.PolicyBundle.BundleId) || !Eq(value.BundleVersion, input.PolicyBundle.Version) ||
            !EqI(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest)) throw new InvalidOperationException("OPA returned a mismatched Automatic Registration decision.");
        if (!value.PolicySignatureValid || value.WorkflowAdvancementAllowed || string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) ||
            value.MaximumClassification > input.MaximumClassification || value.MaximumClassification > identity.Clearance || value.Reasons.IsDefaultOrEmpty ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt || value.Outcome == GovernedIntentPolicyOutcome.Permit && !value.RegistrationAllowed)
            throw new UnauthorizedAccessException("Automatic Registration OPA decision is invalid.");
    }

    private static void ValidateActivation(GovernedAutomaticRegistrationRequest request, GovernedOpenTelemetryActivationReceipt value)
    {
        if (value.ActivationId != request.ActivationId || value.DeliveryRunId != request.DeliveryRunId || !Eq(value.TenantId, request.Identity.TenantId) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted || !value.TelemetryConfigured || !value.ExternalEffectOccurred ||
            value.AutomaticRegistrationOccurred || value.EnterpriseModelMutated || value.CanAdvance || !Eq(value.RuntimeIdentity, request.ExpectedRuntimeIdentity) ||
            !Eq(value.ServiceName, request.ExpectedServiceIdentity) || !Eq(value.ServiceVersion, request.ExpectedServiceVersion) ||
            !EqI(value.ArtifactContentSha256Digest, request.ExpectedArtifactDigest) || !Eq(value.OpenTelemetryEvidenceReference, request.ExpectedOpenTelemetryEvidenceReference) ||
            value.Signals.Length != Enum.GetValues<GovernedTelemetrySignal>().Length || value.Signals.Any(signal => !signal.Configured || !signal.Accepted || string.IsNullOrWhiteSpace(signal.EvidenceReference)) ||
            value.Signals.Select(signal => signal.Signal).Distinct().Count() != Enum.GetValues<GovernedTelemetrySignal>().Length || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Governed OpenTelemetry prerequisite is invalid or mismatched.");
    }

    private static void ValidateRun(GovernedAutomaticRegistrationRequest request, SoftwareDeliveryRun run)
    {
        run.Validate(); var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.OpenTelemetry).ToArray();
        if (run.Id != request.DeliveryRunId || !Eq(run.TenantId, request.Identity.TenantId) || run.CurrentStage != DeliveryStage.OpenTelemetry || run.History.Length != expected.Length ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed || string.IsNullOrWhiteSpace(item.EvidenceReference) || index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run is not stopped at a valid OpenTelemetry boundary.");
    }

    private static void ValidateManifest(GovernedAutomaticRegistrationRequest request, GovernedOpenTelemetryActivationReceipt activation, GovernedAutomaticRegistrationManifest value)
    {
        value.Validate();
        if (value.ManifestId != request.ManifestId || !Eq(value.Version, request.ManifestVersion) || !EqI(value.Sha256Digest, request.ExpectedManifestSha256Digest) ||
            !Eq(value.TenantId, request.Identity.TenantId) || !Eq(value.Environment, request.Environment) || value.ActivationId != request.ActivationId ||
            !Eq(value.RuntimeIdentity, activation.RuntimeIdentity) || !Eq(value.ServiceIdentity, activation.ServiceName) || !Eq(value.ServiceVersion, activation.ServiceVersion) ||
            !EqI(value.ArtifactDigest, activation.ArtifactContentSha256Digest) || !Eq(value.ObservabilityEvidenceReference, activation.OpenTelemetryEvidenceReference) ||
            value.Classification > request.MaximumClassification) throw new UnauthorizedAccessException("Registration manifest is substituted or mismatched.");
    }

    private static void ValidateAuthorization(AutomaticRegistrationResultAuthorizationRequest request, AutomaticRegistrationResultAuthorizationDecision value)
    {
        if (value.AuthorizationRequestId != request.AuthorizationRequestId || value.RegistrationId != request.RegistrationId || !Eq(value.TenantId, request.TenantId) ||
            !EqI(value.RequestFingerprint, request.Commit.RequestFingerprint) || !value.IsAllowed || string.IsNullOrWhiteSpace(value.Code) ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < request.RequestedAt) throw new UnauthorizedAccessException("Registration result authorization is invalid.");
    }

    private static void ValidateEvidence(AutomaticRegistrationEvidenceRecord record, AutomaticRegistrationEvidenceReceipt value)
    {
        if (value.RegistrationId != record.RegistrationId || value.DeliveryRunId != record.DeliveryRunId || !Eq(value.TenantId, record.TenantId) ||
            !EqI(value.RequestFingerprint, record.Commit.RequestFingerprint) || string.IsNullOrWhiteSpace(value.EvidenceReference) || value.RecordedAt < record.AuthorizedAt)
            throw new InvalidOperationException("Automatic Registration evidence receipt is invalid.");
    }

    private static ImmutableArray<string> Normalize(IEnumerable<string> values) => values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
    private static bool Eq(string? left, string? right) => StringComparer.Ordinal.Equals(left, right);
    private static bool EqI(string? left, string? right) => StringComparer.OrdinalIgnoreCase.Equals(left, right);
}
