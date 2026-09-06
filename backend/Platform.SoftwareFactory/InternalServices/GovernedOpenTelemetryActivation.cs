using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.InternalService;

public enum GovernedTelemetrySignal { Traces, Metrics, Logs }

public sealed record GovernedOpenTelemetryProfile(
    Guid ProfileId, string Version, string Sha256Digest, string TenantId,
    string Environment, Guid DeploymentId, string RuntimeIdentity,
    string ServiceName, string ServiceVersion, Uri CollectorAgentEndpoint,
    Uri CollectorGatewayEndpoint, bool TraceAwareRoutingEnabled,
    bool IsLocallyOperated, bool MandatoryExternalControlPlane,
    string TrustAnchorReference, ImmutableHashSet<GovernedTelemetrySignal> RequiredSignals,
    string RedactionPolicyReference, string RedactionPolicySha256Digest,
    string SignatureEvidenceReference, bool SignatureValid,
    ImmutableArray<string> EvidenceReferences)
{
    public GovernedOpenTelemetryProfile Validate()
    {
        if (ProfileId == Guid.Empty || DeploymentId == Guid.Empty)
            throw new InvalidOperationException("Telemetry profile and Deployment identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        GovernedAiPlanningRequest.ValidateDigest(Sha256Digest, "OpenTelemetry profile");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(RuntimeIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceVersion);
        ValidateEndpoint(CollectorAgentEndpoint, "collector agent");
        ValidateEndpoint(CollectorGatewayEndpoint, "collector gateway");
        if (!TraceAwareRoutingEnabled || MandatoryExternalControlPlane)
            throw new InvalidOperationException("Trace-aware routing is required without a mandatory external control plane.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TrustAnchorReference);
        if (!RequiredSignals.SetEquals(Enum.GetValues<GovernedTelemetrySignal>()))
            throw new InvalidOperationException("Traces, metrics, and logs are all required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(RedactionPolicyReference);
        GovernedAiPlanningRequest.ValidateDigest(RedactionPolicySha256Digest, "telemetry redaction policy");
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureEvidenceReference);
        if (!SignatureValid || EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Telemetry profile signature or evidence is invalid.");
        return this;
    }

    private static void ValidateEndpoint(Uri endpoint, string label)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri || !StringComparer.OrdinalIgnoreCase.Equals(endpoint.Scheme, Uri.UriSchemeHttps))
            throw new InvalidOperationException($"The {label} endpoint must be absolute HTTPS.");
    }
}

public sealed record GovernedOpenTelemetryActivationRequest(
    Guid ActivationId, Guid DeploymentId, Guid DeliveryRunId,
    string ExpectedRuntimeIdentity, string ExpectedArtifactContentSha256Digest,
    string ExpectedDeploymentEvidenceReference, bool ExpectedProductionEffectOccurred,
    Guid TelemetryProfileId, string TelemetryProfileVersion,
    string ExpectedTelemetryProfileSha256Digest, string ExpectedServiceName,
    string ExpectedServiceVersion, ImmutableHashSet<GovernedTelemetrySignal> RequiredSignals,
    string ExpectedRedactionPolicyReference, string ExpectedRedactionPolicySha256Digest,
    GovernedIdentity Identity, string Purpose, DataClassification MaximumClassification,
    string AuthorizationEvidenceReference, string Environment,
    IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedOpenTelemetryActivationRequest Validate()
    {
        if (ActivationId == Guid.Empty || DeploymentId == Guid.Empty || DeliveryRunId == Guid.Empty || TelemetryProfileId == Guid.Empty)
            throw new InvalidOperationException("OpenTelemetry and prerequisite identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedRuntimeIdentity);
        GovernedAiPlanningRequest.ValidateDigest(ExpectedArtifactContentSha256Digest, "telemetry Artifact");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedDeploymentEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(TelemetryProfileVersion);
        GovernedAiPlanningRequest.ValidateDigest(ExpectedTelemetryProfileSha256Digest, "OpenTelemetry profile");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedServiceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedServiceVersion);
        if (!RequiredSignals.SetEquals(Enum.GetValues<GovernedTelemetrySignal>()))
            throw new InvalidOperationException("Traces, metrics, and logs are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedRedactionPolicyReference);
        GovernedAiPlanningRequest.ValidateDigest(ExpectedRedactionPolicySha256Digest, "telemetry redaction policy");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("operator.internal-service.telemetry.activate"))
            throw new UnauthorizedAccessException("Governed OpenTelemetry activation permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for telemetry activation.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("OpenTelemetry policy environment or time is invalid.");
        return this;
    }
}

public interface IAuthorizedSovereignDeploymentReceiptReader
{
    Task<GovernedSovereignDeploymentReceipt?> LoadAsync(Guid deploymentId, string tenantId, CancellationToken cancellationToken);
}

public interface IOpenTelemetryDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken);
}

public interface IGovernedOpenTelemetryProfileReader
{
    Task<GovernedOpenTelemetryProfile?> LoadAsync(Guid profileId, string version, string tenantId, CancellationToken cancellationToken);
}

public sealed record OpenTelemetryPolicyInput(
    Guid DecisionRequestId, Guid ActivationId, Guid DeploymentId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, string RuntimeIdentity,
    string ArtifactContentSha256Digest, string DeploymentEvidenceReference,
    bool ProductionEffectOccurred, Guid TelemetryProfileId, string TelemetryProfileVersion,
    string TelemetryProfileSha256Digest, string ServiceName, string ServiceVersion,
    ImmutableHashSet<GovernedTelemetrySignal> RequiredSignals,
    string RedactionPolicyReference, string RedactionPolicySha256Digest,
    IntentPolicyBundleReference PolicyBundle, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset EvaluatedAt);

public sealed record OpenTelemetryPolicyDecision(
    Guid DecisionRequestId, Guid ActivationId, Guid DeploymentId, Guid DeliveryRunId,
    string TenantId, string Environment, string RuntimeIdentity,
    string ArtifactContentSha256Digest, bool ProductionEffectOccurred,
    Guid TelemetryProfileId, string TelemetryProfileVersion,
    string TelemetryProfileSha256Digest, string ServiceName, string ServiceVersion,
    ImmutableHashSet<GovernedTelemetrySignal> AllowedSignals,
    string RedactionPolicyReference, string RedactionPolicySha256Digest,
    bool RegistrationAllowed, bool EnterpriseModelMutationAllowed,
    string BundleId, string BundleVersion, string BundleSha256Digest,
    bool PolicySignatureValid, string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome, DataClassification MaximumClassification,
    ImmutableArray<string> Reasons, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IOpenTelemetryPolicyGate
{
    Task<OpenTelemetryPolicyDecision> EvaluateAsync(OpenTelemetryPolicyInput input, CancellationToken cancellationToken);
}

public sealed record OpenTelemetryRedactionVerificationRequest(
    Guid ActivationId, string TenantId, string RedactionPolicyReference,
    string RedactionPolicySha256Digest, GovernedOpenTelemetryProfile Profile,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record OpenTelemetryRedactionVerificationDecision(
    Guid ActivationId, string TenantId, string RedactionPolicySha256Digest,
    bool IsAllowed, bool SensitiveNamesDropped, bool UnknownAttributesRedacted,
    bool RetainedStringsBounded, bool BaggageClearedAtStart,
    bool BaggageClearedAtEnd, bool LowCardinalityAttributesOnly,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IOpenTelemetryRedactionPolicyVerifier
{
    Task<OpenTelemetryRedactionVerificationDecision> VerifyAsync(
        OpenTelemetryRedactionVerificationRequest request, CancellationToken cancellationToken);
}

public sealed record GovernedTelemetrySignalResult(
    GovernedTelemetrySignal Signal, bool Configured, bool Accepted,
    string EvidenceReference, DateTimeOffset ObservedAt);

public sealed record InstitutionalOpenTelemetryActivationRequest(
    Guid ActivationId, string TenantId, string SubjectId, string Purpose,
    string RuntimeIdentity, string ArtifactContentSha256Digest,
    bool ProductionEffectOccurred, GovernedOpenTelemetryProfile Profile,
    ImmutableHashSet<GovernedTelemetrySignal> RequiredSignals,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record InstitutionalOpenTelemetryActivationResult(
    Guid ActivationId, string TenantId, Guid DeploymentId,
    string RuntimeIdentity, string ArtifactContentSha256Digest,
    string TelemetryProfileSha256Digest, string ServiceName, string ServiceVersion,
    bool ConfigurationApplied, bool ResourceIdentityBound, bool CollectorTrustVerified,
    bool TraceAwareRoutingVerified, bool RedactionEnforced,
    bool BaggageClearedAtStart, bool BaggageClearedAtEnd,
    ImmutableArray<GovernedTelemetrySignalResult> Signals,
    bool ExternalEffectOccurred, bool ProductionEffectOccurred,
    bool AutomaticRegistrationOccurred, bool EnterpriseModelMutated,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset CompletedAt);

public interface IInstitutionalOpenTelemetryGateway
{
    Task<InstitutionalOpenTelemetryActivationResult> ActivateAsync(
        InstitutionalOpenTelemetryActivationRequest request, CancellationToken cancellationToken);
}

public sealed record OpenTelemetryResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid ActivationId, string TenantId, string SubjectId,
    string Purpose, string Environment, DataClassification MaximumClassification,
    InstitutionalOpenTelemetryActivationResult Result,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record OpenTelemetryResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid ActivationId, string TenantId,
    string TelemetryProfileSha256Digest, bool IsAllowed, string Code,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IOpenTelemetryResultAuthorizer
{
    Task<OpenTelemetryResultAuthorizationDecision> AuthorizeAsync(
        OpenTelemetryResultAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record OpenTelemetryEvidenceRecord(
    Guid ActivationId, Guid DeploymentId, Guid DeliveryRunId, string TenantId,
    Guid PolicyDecisionRequestId, GovernedOpenTelemetryProfile Profile,
    InstitutionalOpenTelemetryActivationResult Result,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset AuthorizedAt);

public sealed record OpenTelemetryEvidenceReceipt(
    Guid ActivationId, Guid DeliveryRunId, string TenantId,
    string TelemetryProfileSha256Digest, string EvidenceReference, DateTimeOffset RecordedAt);

public interface IOpenTelemetryEvidenceRecorder
{
    Task<OpenTelemetryEvidenceReceipt> RecordAsync(OpenTelemetryEvidenceRecord record, CancellationToken cancellationToken);
}

public sealed record GovernedOpenTelemetryActivationReceipt(
    Guid ActivationId, Guid DeploymentId, Guid DeliveryRunId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, bool IsAccepted, bool TelemetryConfigured,
    bool ExternalEffectOccurred, bool ProductionEffectOccurred,
    bool AutomaticRegistrationOccurred, bool EnterpriseModelMutated, bool CanAdvance,
    string RuntimeIdentity, string ArtifactContentSha256Digest,
    string TelemetryProfileSha256Digest, string ServiceName, string ServiceVersion,
    ImmutableArray<GovernedTelemetrySignalResult> Signals,
    string? OpenTelemetryEvidenceReference, ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedOpenTelemetryActivationEngine
{
    public async Task<GovernedOpenTelemetryActivationReceipt> ActivateAsync(
        GovernedOpenTelemetryActivationRequest request, IOpenTelemetryPolicyGate policyGate,
        IAuthorizedSovereignDeploymentReceiptReader deploymentReader,
        IOpenTelemetryDeliveryRunReader runReader, IGovernedOpenTelemetryProfileReader profileReader,
        IOpenTelemetryRedactionPolicyVerifier redactionVerifier,
        IInstitutionalOpenTelemetryGateway gateway,
        IOpenTelemetryResultAuthorizer resultAuthorizer,
        IOpenTelemetryEvidenceRecorder evidenceRecorder, CancellationToken cancellationToken)
    {
        request.Validate();
        var input = new OpenTelemetryPolicyInput(
            Guid.NewGuid(), request.ActivationId, request.DeploymentId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, request.ExpectedRuntimeIdentity,
            request.ExpectedArtifactContentSha256Digest, request.ExpectedDeploymentEvidenceReference,
            request.ExpectedProductionEffectOccurred, request.TelemetryProfileId,
            request.TelemetryProfileVersion, request.ExpectedTelemetryProfileSha256Digest,
            request.ExpectedServiceName, request.ExpectedServiceVersion, request.RequiredSignals,
            request.ExpectedRedactionPolicyReference, request.ExpectedRedactionPolicySha256Digest,
            request.PolicyBundle, [request.AuthorizationEvidenceReference, request.ExpectedDeploymentEvidenceReference],
            request.RequestedAt);
        var policy = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidatePolicy(input, request.Identity, policy);
        var policyEvidence = input.EvidenceReferences.Append(policy.PolicyVerificationEvidenceReference)
            .Concat(policy.EvidenceReferences).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        if (policy.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedOpenTelemetryActivationReceipt(
                request.ActivationId, request.DeploymentId, request.DeliveryRunId,
                request.Identity.TenantId, policy.Outcome, false, false, false,
                request.ExpectedProductionEffectOccurred, false, false, false,
                request.ExpectedRuntimeIdentity, request.ExpectedArtifactContentSha256Digest,
                request.ExpectedTelemetryProfileSha256Digest, request.ExpectedServiceName,
                request.ExpectedServiceVersion, [], null, policyEvidence,
                "Policy denial requires a new governed OpenTelemetry request", policy.DecidedAt);

        var deployment = await deploymentReader.LoadAsync(request.DeploymentId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Deployment receipt was not found.");
        ValidateDeployment(request, deployment);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);
        var profile = await profileReader.LoadAsync(
            request.TelemetryProfileId, request.TelemetryProfileVersion,
            request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed OpenTelemetry profile was not found.");
        ValidateProfile(request, profile);
        var prerequisiteEvidence = policyEvidence.Concat(deployment.EvidenceReferences)
            .Append(deployment.DeploymentEvidenceReference!).Concat(profile.EvidenceReferences)
            .Append(profile.SignatureEvidenceReference).Append(run.History[^1].EvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var redactionRequest = new OpenTelemetryRedactionVerificationRequest(
            request.ActivationId, request.Identity.TenantId, request.ExpectedRedactionPolicyReference,
            request.ExpectedRedactionPolicySha256Digest, profile, prerequisiteEvidence, policy.DecidedAt);
        var redaction = await redactionVerifier.VerifyAsync(redactionRequest, cancellationToken);
        ValidateRedaction(redactionRequest, redaction);
        var activationEvidence = prerequisiteEvidence.Concat(redaction.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var activationRequest = new InstitutionalOpenTelemetryActivationRequest(
            request.ActivationId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Purpose, request.ExpectedRuntimeIdentity, request.ExpectedArtifactContentSha256Digest,
            request.ExpectedProductionEffectOccurred, profile, request.RequiredSignals,
            activationEvidence, redaction.DecidedAt);
        var result = await gateway.ActivateAsync(activationRequest, cancellationToken);
        ValidateResult(activationRequest, result);
        var resultEvidence = activationEvidence.Concat(result.EvidenceReferences)
            .Concat(result.Signals.Select(signal => signal.EvidenceReference))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var authorizationRequest = new OpenTelemetryResultAuthorizationRequest(
            Guid.NewGuid(), request.ActivationId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Purpose, request.Environment, request.MaximumClassification,
            result, resultEvidence, result.CompletedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
        ValidateAuthorization(authorizationRequest, authorization);
        var allEvidence = resultEvidence.Concat(authorization.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new OpenTelemetryEvidenceRecord(
            request.ActivationId, request.DeploymentId, request.DeliveryRunId,
            request.Identity.TenantId, policy.DecisionRequestId, profile,
            result, allEvidence, authorization.DecidedAt);
        var evidence = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidence(record, evidence);
        return new GovernedOpenTelemetryActivationReceipt(
            request.ActivationId, request.DeploymentId, request.DeliveryRunId,
            request.Identity.TenantId, policy.Outcome, true, true, true,
            result.ProductionEffectOccurred, false, false, false,
            request.ExpectedRuntimeIdentity, request.ExpectedArtifactContentSha256Digest,
            request.ExpectedTelemetryProfileSha256Digest, request.ExpectedServiceName,
            request.ExpectedServiceVersion, result.Signals, evidence.EvidenceReference,
            allEvidence.Append(evidence.EvidenceReference).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved Automatic Registration", evidence.RecordedAt);
    }

    private static void ValidatePolicy(OpenTelemetryPolicyInput input, GovernedIdentity identity, OpenTelemetryPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.ActivationId != input.ActivationId ||
            value.DeploymentId != input.DeploymentId || value.DeliveryRunId != input.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, input.TenantId) || !StringComparer.Ordinal.Equals(value.Environment, input.Environment) ||
            !StringComparer.Ordinal.Equals(value.RuntimeIdentity, input.RuntimeIdentity) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ArtifactContentSha256Digest, input.ArtifactContentSha256Digest) ||
            value.ProductionEffectOccurred != input.ProductionEffectOccurred || value.TelemetryProfileId != input.TelemetryProfileId ||
            !StringComparer.Ordinal.Equals(value.TelemetryProfileVersion, input.TelemetryProfileVersion) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.TelemetryProfileSha256Digest, input.TelemetryProfileSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.ServiceName, input.ServiceName) || !StringComparer.Ordinal.Equals(value.ServiceVersion, input.ServiceVersion) ||
            !value.AllowedSignals.SetEquals(input.RequiredSignals) || !StringComparer.Ordinal.Equals(value.RedactionPolicyReference, input.RedactionPolicyReference) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.RedactionPolicySha256Digest, input.RedactionPolicySha256Digest) ||
            !StringComparer.Ordinal.Equals(value.BundleId, input.PolicyBundle.BundleId) || !StringComparer.Ordinal.Equals(value.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched OpenTelemetry decision.");
        if (!value.PolicySignatureValid || value.RegistrationAllowed || value.EnterpriseModelMutationAllowed ||
            string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) || value.MaximumClassification > input.MaximumClassification ||
            value.MaximumClassification > identity.Clearance || value.Reasons.IsDefaultOrEmpty ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("OpenTelemetry OPA decision is invalid.");
    }

    private static void ValidateDeployment(GovernedOpenTelemetryActivationRequest request, GovernedSovereignDeploymentReceipt value)
    {
        if (value.DeploymentId != request.DeploymentId || value.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.Ordinal.Equals(value.RuntimeIdentity, request.ExpectedRuntimeIdentity) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ArtifactContentSha256Digest, request.ExpectedArtifactContentSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.DeploymentEvidenceReference, request.ExpectedDeploymentEvidenceReference) ||
            value.ProductionEffectOccurred != request.ExpectedProductionEffectOccurred ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted || !value.DeploymentOccurred ||
            !value.ExternalEffectOccurred || value.TelemetryConfigured || value.AutomaticRegistrationOccurred ||
            value.EnterpriseModelMutated || value.CanAdvance || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Governed Deployment prerequisite is invalid or mismatched.");
    }

    private static void ValidateRun(GovernedOpenTelemetryActivationRequest request, SoftwareDeliveryRun run)
    {
        run.Validate();
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.Deployment).ToArray();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.Deployment || run.History.Length != expected.Length ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) || index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run is not stopped at a valid Deployment boundary.");
    }

    private static void ValidateProfile(GovernedOpenTelemetryActivationRequest request, GovernedOpenTelemetryProfile value)
    {
        value.Validate();
        if (value.ProfileId != request.TelemetryProfileId || !StringComparer.Ordinal.Equals(value.Version, request.TelemetryProfileVersion) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.Sha256Digest, request.ExpectedTelemetryProfileSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) || !StringComparer.Ordinal.Equals(value.Environment, request.Environment) ||
            value.DeploymentId != request.DeploymentId || !StringComparer.Ordinal.Equals(value.RuntimeIdentity, request.ExpectedRuntimeIdentity) ||
            !StringComparer.Ordinal.Equals(value.ServiceName, request.ExpectedServiceName) || !StringComparer.Ordinal.Equals(value.ServiceVersion, request.ExpectedServiceVersion) ||
            !value.RequiredSignals.SetEquals(request.RequiredSignals) || !StringComparer.Ordinal.Equals(value.RedactionPolicyReference, request.ExpectedRedactionPolicyReference) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.RedactionPolicySha256Digest, request.ExpectedRedactionPolicySha256Digest))
            throw new UnauthorizedAccessException("Governed OpenTelemetry profile is substituted or mismatched.");
    }

    private static void ValidateRedaction(OpenTelemetryRedactionVerificationRequest request, OpenTelemetryRedactionVerificationDecision value)
    {
        if (value.ActivationId != request.ActivationId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.RedactionPolicySha256Digest, request.RedactionPolicySha256Digest) || !value.IsAllowed ||
            !value.SensitiveNamesDropped || !value.UnknownAttributesRedacted || !value.RetainedStringsBounded ||
            !value.BaggageClearedAtStart || !value.BaggageClearedAtEnd || !value.LowCardinalityAttributesOnly ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("OpenTelemetry redaction policy verification denied or mismatched.");
    }

    private static void ValidateResult(InstitutionalOpenTelemetryActivationRequest request, InstitutionalOpenTelemetryActivationResult value)
    {
        if (value.ActivationId != request.ActivationId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            value.DeploymentId != request.Profile.DeploymentId || !StringComparer.Ordinal.Equals(value.RuntimeIdentity, request.RuntimeIdentity) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ArtifactContentSha256Digest, request.ArtifactContentSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.TelemetryProfileSha256Digest, request.Profile.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(value.ServiceName, request.Profile.ServiceName) || !StringComparer.Ordinal.Equals(value.ServiceVersion, request.Profile.ServiceVersion) ||
            !value.ConfigurationApplied || !value.ResourceIdentityBound || !value.CollectorTrustVerified || !value.TraceAwareRoutingVerified ||
            !value.RedactionEnforced || !value.BaggageClearedAtStart || !value.BaggageClearedAtEnd || !value.ExternalEffectOccurred ||
            value.ProductionEffectOccurred != request.ProductionEffectOccurred || value.AutomaticRegistrationOccurred || value.EnterpriseModelMutated ||
            value.Signals.GroupBy(signal => signal.Signal).Any(group => group.Count() != 1) ||
            !value.Signals.Select(signal => signal.Signal).ToImmutableHashSet().SetEquals(request.RequiredSignals) ||
            value.Signals.Any(signal => !signal.Configured || !signal.Accepted || string.IsNullOrWhiteSpace(signal.EvidenceReference)) ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.CompletedAt < request.RequestedAt)
            throw new InvalidOperationException("Institutional OpenTelemetry gateway returned an invalid or unsafe proof.");
    }

    private static void ValidateAuthorization(OpenTelemetryResultAuthorizationRequest request, OpenTelemetryResultAuthorizationDecision value)
    {
        if (value.AuthorizationRequestId != request.AuthorizationRequestId || value.ActivationId != request.ActivationId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.TelemetryProfileSha256Digest, request.Result.TelemetryProfileSha256Digest) ||
            !value.IsAllowed || string.IsNullOrWhiteSpace(value.Code) || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("OpenTelemetry result authorization denied or mismatched.");
    }

    private static void ValidateEvidence(OpenTelemetryEvidenceRecord record, OpenTelemetryEvidenceReceipt value)
    {
        if (value.ActivationId != record.ActivationId || value.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.TelemetryProfileSha256Digest, record.Profile.Sha256Digest) ||
            string.IsNullOrWhiteSpace(value.EvidenceReference) || value.RecordedAt < record.AuthorizedAt)
            throw new InvalidOperationException("OpenTelemetry evidence receipt is invalid.");
    }
}
