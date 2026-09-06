using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.InternalService;

public enum GovernedDeploymentTopology { Cloud, PrivateCloud, Hybrid, OnPremises, AirGapped }
public enum GovernedSovereignDependencyKind
{
    ModelRuntime, ArtifactRegistry, PackageRegistry, PolicyAuthority, IdentityProvider,
    EvidenceStore, ObservabilityBackend, SecretsManager, KeyManagement
}

public sealed record GovernedSovereignDependency(
    GovernedSovereignDependencyKind Kind, Uri Endpoint, bool IsLocallyOperated,
    string TrustAnchorReference)
{
    public GovernedSovereignDependency Validate()
    {
        if (!Enum.IsDefined(Kind)) throw new InvalidOperationException("Sovereign dependency kind is invalid.");
        ArgumentNullException.ThrowIfNull(Endpoint);
        if (!Endpoint.IsAbsoluteUri || !StringComparer.OrdinalIgnoreCase.Equals(Endpoint.Scheme, Uri.UriSchemeHttps))
            throw new InvalidOperationException("Sovereign dependency endpoint must use absolute HTTPS.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TrustAnchorReference);
        return this;
    }
}

public sealed record GovernedSovereignDeploymentProfile(
    Guid ProfileId, string Version, string Sha256Digest, string TenantId,
    string EnvironmentName, GovernedDeploymentTopology Topology,
    bool ExternalControlPlaneAllowed, bool ExternalApiAllowed,
    bool ExternalAiServiceAllowed, bool ExternalSaasAllowed,
    bool OutboundNetworkDefaultDeny, ImmutableArray<GovernedSovereignDependency> Dependencies,
    string SignatureEvidenceReference, bool SignatureValid,
    ImmutableArray<string> EvidenceReferences)
{
    public GovernedSovereignDeploymentProfile Validate()
    {
        if (ProfileId == Guid.Empty) throw new InvalidOperationException("Deployment profile identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        GovernedAiPlanningRequest.ValidateDigest(Sha256Digest, "deployment profile");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(EnvironmentName);
        if (!Enum.IsDefined(Topology) || Dependencies.IsDefaultOrEmpty || Dependencies.Any(item => item is null))
            throw new InvalidOperationException("A valid topology and sovereign dependencies are required.");
        var required = Enum.GetValues<GovernedSovereignDependencyKind>();
        if (Dependencies.GroupBy(item => item.Kind).Any(group => group.Count() != 1) ||
            required.Any(kind => Dependencies.All(item => item.Kind != kind)))
            throw new InvalidOperationException("Every sovereign dependency requires exactly one binding.");
        foreach (var dependency in Dependencies) dependency.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureEvidenceReference);
        if (!SignatureValid || EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Deployment profile signature or evidence is invalid.");
        if (Topology == GovernedDeploymentTopology.AirGapped &&
            (ExternalControlPlaneAllowed || ExternalApiAllowed || ExternalAiServiceAllowed || ExternalSaasAllowed ||
             !OutboundNetworkDefaultDeny || Dependencies.Any(item => !item.IsLocallyOperated)))
            throw new InvalidOperationException("Air-gapped deployment requires local dependencies, default-deny outbound networking, and no external control plane, API, AI, or SaaS dependency.");
        return this;
    }
}

public sealed record GovernedVerifiedDeploymentArtifact(
    Guid PublicationId, GovernedArtifactCoordinate Coordinate, string ContentSha256Digest,
    string ImmutableRegistryReference, string SbomReference, string BuildAttestationReference,
    string SignatureReference, string SupplyChainVerificationEvidenceReference,
    ImmutableArray<string> EvidenceReferences)
{
    public GovernedVerifiedDeploymentArtifact Validate()
    {
        if (PublicationId == Guid.Empty) throw new InvalidOperationException("Artifact publication identity is required.");
        Coordinate.Validate();
        GovernedAiPlanningRequest.ValidateDigest(ContentSha256Digest, "deployment artifact");
        ArgumentException.ThrowIfNullOrWhiteSpace(ImmutableRegistryReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SbomReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(BuildAttestationReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SupplyChainVerificationEvidenceReference);
        if (EvidenceReferences.IsDefaultOrEmpty) throw new InvalidOperationException("Deployment Artifact evidence is required.");
        return this;
    }
}

public sealed record GovernedSovereignDeploymentRequest(
    Guid DeploymentId, Guid ArtifactPublicationId, Guid DeliveryRunId,
    string ExpectedArtifactContentSha256Digest, string ExpectedImmutableRegistryReference,
    string ExpectedArtifactEvidenceReference, Guid DeploymentProfileId,
    string DeploymentProfileVersion, string ExpectedDeploymentProfileSha256Digest,
    string TargetEnvironment, bool ProductionDeploymentRequested,
    string HumanApprovalReference, string WorkloadIdentityReference,
    string SecretsPolicyReference, string RollbackPolicyReference,
    GovernedIdentity Identity, string Purpose, DataClassification MaximumClassification,
    string AuthorizationEvidenceReference, string Environment,
    IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedSovereignDeploymentRequest Validate()
    {
        if (DeploymentId == Guid.Empty || ArtifactPublicationId == Guid.Empty ||
            DeliveryRunId == Guid.Empty || DeploymentProfileId == Guid.Empty)
            throw new InvalidOperationException("Deployment and prerequisite identities are required.");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedArtifactContentSha256Digest, "deployment artifact");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedImmutableRegistryReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedArtifactEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(DeploymentProfileVersion);
        GovernedAiPlanningRequest.ValidateDigest(ExpectedDeploymentProfileSha256Digest, "deployment profile");
        ArgumentException.ThrowIfNullOrWhiteSpace(TargetEnvironment);
        ArgumentException.ThrowIfNullOrWhiteSpace(HumanApprovalReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(WorkloadIdentityReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SecretsPolicyReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(RollbackPolicyReference);
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("operator.internal-service.deployment.execute"))
            throw new UnauthorizedAccessException("Governed Deployment permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Deployment.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Deployment policy environment or time is invalid.");
        return this;
    }
}

public interface IAuthorizedArtifactPublicationReceiptReader
{
    Task<GovernedArtifactPublicationReceipt?> LoadAsync(Guid publicationId, string tenantId, CancellationToken cancellationToken);
}

public interface IAuthorizedDeploymentArtifactReader
{
    Task<GovernedVerifiedDeploymentArtifact?> LoadAsync(Guid publicationId, string tenantId, CancellationToken cancellationToken);
}

public interface IDeploymentDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken);
}

public interface IGovernedSovereignDeploymentProfileReader
{
    Task<GovernedSovereignDeploymentProfile?> LoadAsync(
        Guid profileId, string version, string tenantId, CancellationToken cancellationToken);
}

public sealed record DeploymentPolicyInput(
    Guid DecisionRequestId, Guid DeploymentId, Guid ArtifactPublicationId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, string ArtifactContentSha256Digest,
    string ImmutableRegistryReference, string ArtifactEvidenceReference,
    Guid DeploymentProfileId, string DeploymentProfileVersion, string DeploymentProfileSha256Digest,
    string TargetEnvironment, bool ProductionDeploymentRequested, string HumanApprovalReference,
    string WorkloadIdentityReference, string SecretsPolicyReference, string RollbackPolicyReference,
    IntentPolicyBundleReference PolicyBundle, ImmutableArray<string> EvidenceReferences, DateTimeOffset EvaluatedAt);

public sealed record DeploymentPolicyDecision(
    Guid DecisionRequestId, Guid DeploymentId, Guid ArtifactPublicationId, Guid DeliveryRunId,
    string TenantId, string Environment, string ArtifactContentSha256Digest,
    string ImmutableRegistryReference, Guid DeploymentProfileId, string DeploymentProfileVersion,
    string DeploymentProfileSha256Digest, string TargetEnvironment, bool ProductionDeploymentAllowed,
    string HumanApprovalReference, string WorkloadIdentityReference, string SecretsPolicyReference,
    string RollbackPolicyReference, bool HumanApprovalValid, bool AiAuthorityDetected,
    string BundleId, string BundleVersion, string BundleSha256Digest, bool PolicySignatureValid,
    string PolicyVerificationEvidenceReference, GovernedIntentPolicyOutcome Outcome,
    DataClassification MaximumClassification, ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IDeploymentPolicyGate
{
    Task<DeploymentPolicyDecision> EvaluateAsync(DeploymentPolicyInput input, CancellationToken cancellationToken);
}

public sealed record DeploymentPreflightRequest(
    Guid DeploymentId, string TenantId, string TargetEnvironment,
    bool ProductionDeploymentRequested, GovernedVerifiedDeploymentArtifact Artifact,
    GovernedSovereignDeploymentProfile Profile, string WorkloadIdentityReference,
    string SecretsPolicyReference, string RollbackPolicyReference,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record DeploymentPreflightDecision(
    Guid DeploymentId, string TenantId, string ArtifactContentSha256Digest,
    string DeploymentProfileSha256Digest, string TargetEnvironment, bool IsAllowed,
    bool ArtifactAvailable, bool WorkloadIdentityValid, bool SecretsReferencesValid,
    bool TargetIsolationValid, bool RollbackReady, bool CapacityPolicySatisfied,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IInstitutionalDeploymentPreflightValidator
{
    Task<DeploymentPreflightDecision> ValidateAsync(DeploymentPreflightRequest request, CancellationToken cancellationToken);
}

public sealed record InstitutionalDeploymentExecutionRequest(
    Guid DeploymentId, string TenantId, string SubjectId, string Purpose,
    string TargetEnvironment, bool ProductionDeploymentRequested,
    GovernedVerifiedDeploymentArtifact Artifact, GovernedSovereignDeploymentProfile Profile,
    string WorkloadIdentityReference, string SecretsPolicyReference,
    string RollbackPolicyReference, string HumanApprovalReference,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record InstitutionalDeploymentExecutionResult(
    Guid DeploymentId, string TenantId, Guid DeploymentProfileId,
    string TargetEnvironment, string ArtifactContentSha256Digest,
    string ImmutableRegistryReference, string RuntimeIdentity, bool DeploymentOccurred,
    bool ActivationVerified, bool IdempotencyVerified, string RollbackReference,
    bool ExternalEffectOccurred, bool ProductionEffectOccurred,
    bool TelemetryConfigured, bool AutomaticRegistrationOccurred,
    bool EnterpriseModelMutated, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CompletedAt);

public interface IInstitutionalSovereignDeploymentGateway
{
    Task<InstitutionalDeploymentExecutionResult> DeployAsync(
        InstitutionalDeploymentExecutionRequest request, CancellationToken cancellationToken);
}

public sealed record DeploymentResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid DeploymentId, string TenantId, string SubjectId,
    string Purpose, string Environment, DataClassification MaximumClassification,
    InstitutionalDeploymentExecutionResult Result, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RequestedAt);

public sealed record DeploymentResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid DeploymentId, string TenantId,
    string ArtifactContentSha256Digest, bool IsAllowed, string Code,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IDeploymentResultAuthorizer
{
    Task<DeploymentResultAuthorizationDecision> AuthorizeAsync(
        DeploymentResultAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record DeploymentEvidenceRecord(
    Guid DeploymentId, Guid ArtifactPublicationId, Guid DeliveryRunId,
    string TenantId, Guid PolicyDecisionRequestId, GovernedVerifiedDeploymentArtifact Artifact,
    GovernedSovereignDeploymentProfile Profile, InstitutionalDeploymentExecutionResult Result,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset AuthorizedAt);

public sealed record DeploymentEvidenceReceipt(
    Guid DeploymentId, Guid DeliveryRunId, string TenantId,
    string ArtifactContentSha256Digest, string RuntimeIdentity,
    string EvidenceReference, DateTimeOffset RecordedAt);

public interface IDeploymentEvidenceRecorder
{
    Task<DeploymentEvidenceReceipt> RecordAsync(DeploymentEvidenceRecord record, CancellationToken cancellationToken);
}

public sealed record GovernedSovereignDeploymentReceipt(
    Guid DeploymentId, Guid ArtifactPublicationId, Guid DeliveryRunId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, bool IsAccepted, bool DeploymentOccurred,
    bool ExternalEffectOccurred, bool ProductionEffectOccurred, bool TelemetryConfigured,
    bool AutomaticRegistrationOccurred, bool EnterpriseModelMutated, bool CanAdvance,
    string ArtifactContentSha256Digest, Guid DeploymentProfileId, string TargetEnvironment,
    string? RuntimeIdentity, string? RollbackReference, string? DeploymentEvidenceReference,
    ImmutableArray<string> EvidenceReferences, string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedSovereignDeploymentEngine
{
    public async Task<GovernedSovereignDeploymentReceipt> DeployAsync(
        GovernedSovereignDeploymentRequest request, IDeploymentPolicyGate policyGate,
        IAuthorizedArtifactPublicationReceiptReader artifactReceiptReader,
        IAuthorizedDeploymentArtifactReader artifactReader, IDeploymentDeliveryRunReader runReader,
        IGovernedSovereignDeploymentProfileReader profileReader,
        IInstitutionalDeploymentPreflightValidator preflightValidator,
        IInstitutionalSovereignDeploymentGateway gateway,
        IDeploymentResultAuthorizer resultAuthorizer,
        IDeploymentEvidenceRecorder evidenceRecorder, CancellationToken cancellationToken)
    {
        request.Validate();
        var input = new DeploymentPolicyInput(
            Guid.NewGuid(), request.DeploymentId, request.ArtifactPublicationId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, request.ExpectedArtifactContentSha256Digest,
            request.ExpectedImmutableRegistryReference, request.ExpectedArtifactEvidenceReference,
            request.DeploymentProfileId, request.DeploymentProfileVersion,
            request.ExpectedDeploymentProfileSha256Digest, request.TargetEnvironment,
            request.ProductionDeploymentRequested, request.HumanApprovalReference,
            request.WorkloadIdentityReference, request.SecretsPolicyReference,
            request.RollbackPolicyReference, request.PolicyBundle,
            [request.AuthorizationEvidenceReference, request.HumanApprovalReference], request.RequestedAt);
        var policy = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidatePolicy(input, request.Identity, policy);
        var policyEvidence = input.EvidenceReferences.Append(policy.PolicyVerificationEvidenceReference)
            .Concat(policy.EvidenceReferences).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        if (policy.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedSovereignDeploymentReceipt(
                request.DeploymentId, request.ArtifactPublicationId, request.DeliveryRunId,
                request.Identity.TenantId, policy.Outcome, false, false, false, false,
                false, false, false, false, request.ExpectedArtifactContentSha256Digest,
                request.DeploymentProfileId, request.TargetEnvironment, null, null, null,
                policyEvidence, "Policy denial requires a new governed Deployment request", policy.DecidedAt);

        var artifactReceipt = await artifactReceiptReader.LoadAsync(
            request.ArtifactPublicationId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Artifact receipt was not found.");
        ValidateArtifactReceipt(request, artifactReceipt);
        var artifact = await artifactReader.LoadAsync(
            request.ArtifactPublicationId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Verified deployment Artifact was not found.");
        ValidateArtifact(request, artifact);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);
        var profile = await profileReader.LoadAsync(
            request.DeploymentProfileId, request.DeploymentProfileVersion,
            request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed sovereign Deployment profile was not found.");
        ValidateProfile(request, profile);
        var prerequisiteEvidence = policyEvidence.Concat(artifactReceipt.EvidenceReferences)
            .Append(artifactReceipt.ArtifactEvidenceReference!).Concat(artifact.EvidenceReferences)
            .Concat(profile.EvidenceReferences).Append(profile.SignatureEvidenceReference)
            .Append(run.History[^1].EvidenceReference).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        var preflightRequest = new DeploymentPreflightRequest(
            request.DeploymentId, request.Identity.TenantId, request.TargetEnvironment,
            request.ProductionDeploymentRequested, artifact, profile,
            request.WorkloadIdentityReference, request.SecretsPolicyReference,
            request.RollbackPolicyReference, prerequisiteEvidence, policy.DecidedAt);
        var preflight = await preflightValidator.ValidateAsync(preflightRequest, cancellationToken);
        ValidatePreflight(preflightRequest, preflight);
        var executionEvidence = prerequisiteEvidence.Concat(preflight.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var executionRequest = new InstitutionalDeploymentExecutionRequest(
            request.DeploymentId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Purpose, request.TargetEnvironment, request.ProductionDeploymentRequested,
            artifact, profile, request.WorkloadIdentityReference, request.SecretsPolicyReference,
            request.RollbackPolicyReference, request.HumanApprovalReference,
            executionEvidence, preflight.DecidedAt);
        var result = await gateway.DeployAsync(executionRequest, cancellationToken);
        ValidateResult(executionRequest, result);
        var resultEvidence = executionEvidence.Concat(result.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var authorizationRequest = new DeploymentResultAuthorizationRequest(
            Guid.NewGuid(), request.DeploymentId, request.Identity.TenantId,
            request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, result, resultEvidence, result.CompletedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
        ValidateAuthorization(authorizationRequest, authorization);
        var allEvidence = resultEvidence.Concat(authorization.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new DeploymentEvidenceRecord(
            request.DeploymentId, request.ArtifactPublicationId, request.DeliveryRunId,
            request.Identity.TenantId, policy.DecisionRequestId, artifact, profile,
            result, allEvidence, authorization.DecidedAt);
        var evidence = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidence(record, evidence);
        return new GovernedSovereignDeploymentReceipt(
            request.DeploymentId, request.ArtifactPublicationId, request.DeliveryRunId,
            request.Identity.TenantId, policy.Outcome, true, true, true,
            result.ProductionEffectOccurred, false, false, false, false,
            request.ExpectedArtifactContentSha256Digest, request.DeploymentProfileId,
            request.TargetEnvironment, result.RuntimeIdentity, result.RollbackReference,
            evidence.EvidenceReference, allEvidence.Append(evidence.EvidenceReference)
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved OpenTelemetry", evidence.RecordedAt);
    }

    private static void ValidatePolicy(DeploymentPolicyInput input, GovernedIdentity identity, DeploymentPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.DeploymentId != input.DeploymentId ||
            value.ArtifactPublicationId != input.ArtifactPublicationId || value.DeliveryRunId != input.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, input.TenantId) || !StringComparer.Ordinal.Equals(value.Environment, input.Environment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ArtifactContentSha256Digest, input.ArtifactContentSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.ImmutableRegistryReference, input.ImmutableRegistryReference) ||
            value.DeploymentProfileId != input.DeploymentProfileId || !StringComparer.Ordinal.Equals(value.DeploymentProfileVersion, input.DeploymentProfileVersion) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.DeploymentProfileSha256Digest, input.DeploymentProfileSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.TargetEnvironment, input.TargetEnvironment) ||
            value.ProductionDeploymentAllowed != input.ProductionDeploymentRequested ||
            !StringComparer.Ordinal.Equals(value.HumanApprovalReference, input.HumanApprovalReference) ||
            !StringComparer.Ordinal.Equals(value.WorkloadIdentityReference, input.WorkloadIdentityReference) ||
            !StringComparer.Ordinal.Equals(value.SecretsPolicyReference, input.SecretsPolicyReference) ||
            !StringComparer.Ordinal.Equals(value.RollbackPolicyReference, input.RollbackPolicyReference) ||
            !StringComparer.Ordinal.Equals(value.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(value.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Deployment decision.");
        if (!value.PolicySignatureValid || !value.HumanApprovalValid || value.AiAuthorityDetected ||
            string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) ||
            value.MaximumClassification > input.MaximumClassification || value.MaximumClassification > identity.Clearance ||
            value.Reasons.IsDefaultOrEmpty || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("Deployment OPA decision is invalid.");
    }

    private static void ValidateArtifactReceipt(GovernedSovereignDeploymentRequest request, GovernedArtifactPublicationReceipt value)
    {
        if (value.PublicationId != request.ArtifactPublicationId || value.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ContentSha256Digest, request.ExpectedArtifactContentSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.ImmutableRegistryReference, request.ExpectedImmutableRegistryReference) ||
            !StringComparer.Ordinal.Equals(value.ArtifactEvidenceReference, request.ExpectedArtifactEvidenceReference) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted || !value.ArtifactPublished ||
            !value.RegistryMutated || !value.SupplyChainVerified || value.SupplyChainVerifications.Any(item => !item.Passed) ||
            value.DeploymentOccurred || value.ProductionEffectOccurred || value.CanAdvance || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Governed Artifact prerequisite is invalid or mismatched.");
    }

    private static void ValidateArtifact(GovernedSovereignDeploymentRequest request, GovernedVerifiedDeploymentArtifact value)
    {
        value.Validate();
        if (value.PublicationId != request.ArtifactPublicationId ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ContentSha256Digest, request.ExpectedArtifactContentSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.ImmutableRegistryReference, request.ExpectedImmutableRegistryReference) ||
            !StringComparer.Ordinal.Equals(value.SupplyChainVerificationEvidenceReference, request.ExpectedArtifactEvidenceReference))
            throw new UnauthorizedAccessException("Verified deployment Artifact is substituted or mismatched.");
    }

    private static void ValidateRun(GovernedSovereignDeploymentRequest request, SoftwareDeliveryRun run)
    {
        run.Validate();
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.Artifact).ToArray();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.Artifact || run.History.Length != expected.Length ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) || index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run is not stopped at a valid Artifact boundary.");
    }

    private static void ValidateProfile(GovernedSovereignDeploymentRequest request, GovernedSovereignDeploymentProfile value)
    {
        value.Validate();
        if (value.ProfileId != request.DeploymentProfileId || !StringComparer.Ordinal.Equals(value.Version, request.DeploymentProfileVersion) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.Sha256Digest, request.ExpectedDeploymentProfileSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.Ordinal.Equals(value.EnvironmentName, request.TargetEnvironment))
            throw new UnauthorizedAccessException("Sovereign Deployment profile is substituted or mismatched.");
    }

    private static void ValidatePreflight(DeploymentPreflightRequest request, DeploymentPreflightDecision value)
    {
        if (value.DeploymentId != request.DeploymentId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ArtifactContentSha256Digest, request.Artifact.ContentSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.DeploymentProfileSha256Digest, request.Profile.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(value.TargetEnvironment, request.TargetEnvironment) || !value.IsAllowed ||
            !value.ArtifactAvailable || !value.WorkloadIdentityValid || !value.SecretsReferencesValid ||
            !value.TargetIsolationValid || !value.RollbackReady || !value.CapacityPolicySatisfied ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Institutional Deployment preflight denied or mismatched.");
    }

    private static void ValidateResult(InstitutionalDeploymentExecutionRequest request, InstitutionalDeploymentExecutionResult value)
    {
        if (value.DeploymentId != request.DeploymentId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            value.DeploymentProfileId != request.Profile.ProfileId || !StringComparer.Ordinal.Equals(value.TargetEnvironment, request.TargetEnvironment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ArtifactContentSha256Digest, request.Artifact.ContentSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.ImmutableRegistryReference, request.Artifact.ImmutableRegistryReference) ||
            string.IsNullOrWhiteSpace(value.RuntimeIdentity) || !value.DeploymentOccurred || !value.ActivationVerified ||
            !value.IdempotencyVerified || string.IsNullOrWhiteSpace(value.RollbackReference) || !value.ExternalEffectOccurred ||
            value.ProductionEffectOccurred != request.ProductionDeploymentRequested || value.TelemetryConfigured ||
            value.AutomaticRegistrationOccurred || value.EnterpriseModelMutated || value.EvidenceReferences.IsDefaultOrEmpty ||
            value.CompletedAt < request.RequestedAt)
            throw new InvalidOperationException("Institutional Deployment gateway returned an invalid or unsafe proof.");
    }

    private static void ValidateAuthorization(DeploymentResultAuthorizationRequest request, DeploymentResultAuthorizationDecision value)
    {
        if (value.AuthorizationRequestId != request.AuthorizationRequestId || value.DeploymentId != request.DeploymentId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ArtifactContentSha256Digest, request.Result.ArtifactContentSha256Digest) ||
            !value.IsAllowed || string.IsNullOrWhiteSpace(value.Code) || value.EvidenceReferences.IsDefaultOrEmpty ||
            value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Deployment result authorization denied or mismatched.");
    }

    private static void ValidateEvidence(DeploymentEvidenceRecord record, DeploymentEvidenceReceipt value)
    {
        if (value.DeploymentId != record.DeploymentId || value.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ArtifactContentSha256Digest, record.Result.ArtifactContentSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.RuntimeIdentity, record.Result.RuntimeIdentity) ||
            string.IsNullOrWhiteSpace(value.EvidenceReference) || value.RecordedAt < record.AuthorizedAt)
            throw new InvalidOperationException("Deployment evidence receipt is invalid.");
    }
}
