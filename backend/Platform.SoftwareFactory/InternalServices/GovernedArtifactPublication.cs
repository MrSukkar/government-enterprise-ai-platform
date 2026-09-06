using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.SupplyChain;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedArtifactCoordinate(string Name, string Version, string Kind)
{
    public GovernedArtifactCoordinate Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(Kind);
        if (new[] { Name, Version, Kind }.Any(value => value != value.Trim() || value.Any(char.IsControl)))
            throw new InvalidOperationException("Artifact coordinate is invalid.");
        return this;
    }

    public override string ToString() => $"{Kind}:{Name}:{Version}";
}

public sealed record GovernedArtifactPublicationRequest(
    Guid PublicationId, Guid CiCdExecutionId, Guid DeliveryRunId,
    string ExpectedPipelineManifestSha256Digest, string ExpectedSourceCommitId,
    string ExpectedWorkflowSha256Digest, GovernedArtifactCoordinate Coordinate,
    string ExpectedContentSha256Digest, string RegistryId, string RegistryRepository,
    string SigningPolicyReference, ImmutableHashSet<SupplyChainControl> RequiredControls,
    GovernedIdentity Identity, string Purpose, DataClassification MaximumClassification,
    string AuthorizationEvidenceReference, string Environment,
    IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedArtifactPublicationRequest Validate()
    {
        if (PublicationId == Guid.Empty || CiCdExecutionId == Guid.Empty || DeliveryRunId == Guid.Empty)
            throw new InvalidOperationException("Artifact and prerequisite identities are required.");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedPipelineManifestSha256Digest, "pipeline output manifest");
        GovernedGitSourceCommitRequest.ValidateObjectId(ExpectedSourceCommitId, "artifact source commit");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedWorkflowSha256Digest, "artifact workflow");
        Coordinate.Validate();
        GovernedAiPlanningRequest.ValidateDigest(ExpectedContentSha256Digest, "artifact content");
        ArgumentException.ThrowIfNullOrWhiteSpace(RegistryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(RegistryRepository);
        ArgumentException.ThrowIfNullOrWhiteSpace(SigningPolicyReference);
        var mandatory = Enum.GetValues<SupplyChainControl>().ToImmutableHashSet();
        if (!RequiredControls.SetEquals(mandatory))
            throw new InvalidOperationException("Every mandatory supply-chain control is required.");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("developer.internal-service.artifact.publish"))
            throw new UnauthorizedAccessException("Governed Artifact publication permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Artifact publication.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Artifact policy environment or time is invalid.");
        return this;
    }
}

public interface IAuthorizedCiCdExecutionReceiptReader
{
    Task<GovernedCiCdExecutionReceipt?> LoadAsync(Guid executionId, string tenantId, CancellationToken cancellationToken);
}

public interface IAuthorizedPipelineOutputManifestReader
{
    Task<GovernedPipelineOutputManifest?> LoadAsync(
        Guid executionId, string manifestSha256Digest, string tenantId, CancellationToken cancellationToken);
}

public interface IArtifactDeliveryRunReader
{
    Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken);
}

public sealed record ArtifactPolicyInput(
    Guid DecisionRequestId, Guid PublicationId, Guid CiCdExecutionId, Guid DeliveryRunId,
    string TenantId, string SubjectId, string Purpose, string Environment,
    DataClassification MaximumClassification, string PipelineManifestSha256Digest,
    string SourceCommitId, string WorkflowSha256Digest, GovernedArtifactCoordinate Coordinate,
    string ContentSha256Digest, string RegistryId, string RegistryRepository,
    string SigningPolicyReference, ImmutableHashSet<SupplyChainControl> RequiredControls,
    IntentPolicyBundleReference PolicyBundle, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset EvaluatedAt);

public sealed record ArtifactPolicyDecision(
    Guid DecisionRequestId, Guid PublicationId, Guid CiCdExecutionId, Guid DeliveryRunId,
    string TenantId, string Environment, string PipelineManifestSha256Digest,
    string SourceCommitId, string WorkflowSha256Digest, GovernedArtifactCoordinate Coordinate,
    string ContentSha256Digest, string RegistryId, string RegistryRepository,
    string SigningPolicyReference, ImmutableHashSet<SupplyChainControl> AllowedControls,
    bool OverwriteAllowed, bool DeploymentAllowed, string BundleId, string BundleVersion,
    string BundleSha256Digest, bool PolicySignatureValid,
    string PolicyVerificationEvidenceReference, GovernedIntentPolicyOutcome Outcome,
    DataClassification MaximumClassification, ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IArtifactPolicyGate
{
    Task<ArtifactPolicyDecision> EvaluateAsync(ArtifactPolicyInput input, CancellationToken cancellationToken);
}

public sealed record ArtifactPackageValidationRequest(
    Guid PublicationId, string TenantId, GovernedArtifactCoordinate Coordinate,
    string ExpectedContentSha256Digest, GovernedPipelineOutputManifest Manifest,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record ArtifactPackageValidationDecision(
    Guid PublicationId, string TenantId, string PipelineManifestSha256Digest,
    string ContentSha256Digest, bool IsAllowed, bool PartialOutputDetected,
    bool DigestSubstitutionDetected, bool ProvenanceMismatchDetected,
    bool SbomMismatchDetected, bool AttestationMismatchDetected,
    bool SignatureMismatchDetected, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IArtifactPackageValidator
{
    Task<ArtifactPackageValidationDecision> ValidateAsync(
        ArtifactPackageValidationRequest request, CancellationToken cancellationToken);
}

public sealed record InstitutionalArtifactPublicationRequest(
    Guid PublicationId, string TenantId, string SubjectId,
    GovernedArtifactCoordinate Coordinate, string ContentSha256Digest,
    string RegistryId, string RegistryRepository, string SigningPolicyReference,
    GovernedPipelineOutputManifest Manifest, ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RequestedAt);

public sealed record InstitutionalArtifactPublicationResult(
    Guid PublicationId, string TenantId, GovernedArtifactCoordinate Coordinate,
    string ContentSha256Digest, string RegistryId, string RegistryRepository,
    string ImmutableRegistryReference, bool Published, bool RegistryMutated,
    bool ImmutableCoordinate, bool ExistingCoordinateOverwritten, bool IdempotencyVerified,
    bool ArtifactSigned, string SignatureReference, bool SourceMutationOccurred,
    bool DeploymentOccurred, bool ProductionEffectOccurred,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset PublishedAt);

public interface IInstitutionalArtifactRegistryGateway
{
    Task<InstitutionalArtifactPublicationResult> PublishAsync(
        InstitutionalArtifactPublicationRequest request, CancellationToken cancellationToken);
}

public sealed record ArtifactResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid PublicationId, string TenantId, string SubjectId,
    string Purpose, string Environment, DataClassification MaximumClassification,
    InstitutionalArtifactPublicationResult Publication, SupplyChainVerificationReport Verification,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);

public sealed record ArtifactResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid PublicationId, string TenantId,
    string ContentSha256Digest, bool IsAllowed, string Code,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);

public interface IArtifactResultAuthorizer
{
    Task<ArtifactResultAuthorizationDecision> AuthorizeAsync(
        ArtifactResultAuthorizationRequest request, CancellationToken cancellationToken);
}

public sealed record ArtifactEvidenceRecord(
    Guid PublicationId, Guid CiCdExecutionId, Guid DeliveryRunId, string TenantId,
    Guid PolicyDecisionRequestId, GovernedPipelineOutputManifest Manifest,
    InstitutionalArtifactPublicationResult Publication, SupplyChainVerificationReport Verification,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset AuthorizedAt);

public sealed record ArtifactEvidenceReceipt(
    Guid PublicationId, Guid DeliveryRunId, string TenantId, string ContentSha256Digest,
    string ImmutableRegistryReference, string EvidenceReference, DateTimeOffset RecordedAt);

public interface IArtifactEvidenceRecorder
{
    Task<ArtifactEvidenceReceipt> RecordAsync(ArtifactEvidenceRecord record, CancellationToken cancellationToken);
}

public sealed record GovernedArtifactPublicationReceipt(
    Guid PublicationId, Guid CiCdExecutionId, Guid DeliveryRunId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, bool IsAccepted, bool ArtifactPublished,
    bool RegistryMutated, bool SourceMutationOccurred, bool DeploymentOccurred,
    bool ProductionEffectOccurred, bool CanAdvance, GovernedArtifactCoordinate Coordinate,
    string ContentSha256Digest, string? ImmutableRegistryReference,
    bool SupplyChainVerified, ImmutableArray<SupplyChainVerification> SupplyChainVerifications,
    string? ArtifactEvidenceReference, ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedArtifactPublicationEngine
{
    public async Task<GovernedArtifactPublicationReceipt> PublishAsync(
        GovernedArtifactPublicationRequest request, IArtifactPolicyGate policyGate,
        IAuthorizedCiCdExecutionReceiptReader ciCdReader,
        IAuthorizedPipelineOutputManifestReader manifestReader, IArtifactDeliveryRunReader runReader,
        IArtifactPackageValidator packageValidator, IInstitutionalArtifactRegistryGateway registryGateway,
        SupplyChainVerificationPipeline supplyChainPipeline, IArtifactResultAuthorizer resultAuthorizer,
        IArtifactEvidenceRecorder evidenceRecorder, CancellationToken cancellationToken)
    {
        request.Validate();
        var input = new ArtifactPolicyInput(
            Guid.NewGuid(), request.PublicationId, request.CiCdExecutionId, request.DeliveryRunId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, request.ExpectedPipelineManifestSha256Digest,
            request.ExpectedSourceCommitId, request.ExpectedWorkflowSha256Digest, request.Coordinate,
            request.ExpectedContentSha256Digest, request.RegistryId, request.RegistryRepository,
            request.SigningPolicyReference, request.RequiredControls, request.PolicyBundle,
            [request.AuthorizationEvidenceReference], request.RequestedAt);
        var policy = await policyGate.EvaluateAsync(input, cancellationToken);
        ValidatePolicy(input, request.Identity, policy);
        var policyEvidence = input.EvidenceReferences.Append(policy.PolicyVerificationEvidenceReference)
            .Concat(policy.EvidenceReferences).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        if (policy.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new GovernedArtifactPublicationReceipt(
                request.PublicationId, request.CiCdExecutionId, request.DeliveryRunId,
                request.Identity.TenantId, policy.Outcome, false, false, false, false, false, false, false,
                request.Coordinate, request.ExpectedContentSha256Digest, null, false, [], null,
                policyEvidence, "Policy denial requires a new governed Artifact request", policy.DecidedAt);

        var ciCd = await ciCdReader.LoadAsync(request.CiCdExecutionId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed CI/CD receipt was not found.");
        ValidateCiCd(request, ciCd);
        var manifest = await manifestReader.LoadAsync(
            request.CiCdExecutionId, request.ExpectedPipelineManifestSha256Digest,
            request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Authorized pipeline-output manifest was not found.");
        ValidateManifest(request, manifest);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);
        var prerequisiteEvidence = policyEvidence.Concat(ciCd.EvidenceReferences)
            .Append(ciCd.CiCdEvidenceReference!).Concat(manifest.EvidenceReferences)
            .Append(run.History[^1].EvidenceReference).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        var packageRequest = new ArtifactPackageValidationRequest(
            request.PublicationId, request.Identity.TenantId, request.Coordinate,
            request.ExpectedContentSha256Digest, manifest, prerequisiteEvidence, policy.DecidedAt);
        var packageDecision = await packageValidator.ValidateAsync(packageRequest, cancellationToken);
        ValidatePackage(packageRequest, packageDecision);
        var publicationEvidence = prerequisiteEvidence.Concat(packageDecision.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var publicationRequest = new InstitutionalArtifactPublicationRequest(
            request.PublicationId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Coordinate, request.ExpectedContentSha256Digest, request.RegistryId,
            request.RegistryRepository, request.SigningPolicyReference, manifest,
            publicationEvidence, packageDecision.DecidedAt);
        var publication = await registryGateway.PublishAsync(publicationRequest, cancellationToken);
        ValidatePublication(publicationRequest, publication);
        var artifact = new ArtifactSupplyChainRecord(
            request.Coordinate.ToString(), $"sha256:{request.ExpectedContentSha256Digest.ToLowerInvariant()}",
            manifest.RepositoryId, manifest.CommitId, manifest.SbomReference,
            manifest.BuildAttestationReference, publication.SignatureReference,
            publication.ImmutableRegistryReference);
        var verification = await supplyChainPipeline.VerifyAsync(artifact, cancellationToken);
        if (!verification.IsVerified || !verification.Verifications.Select(item => item.Control)
                .ToImmutableHashSet().SetEquals(request.RequiredControls))
            throw new UnauthorizedAccessException("Artifact supply-chain verification failed or was incomplete.");
        var verifiedEvidence = publicationEvidence.Append(publication.SignatureReference)
            .Concat(publication.EvidenceReferences).Concat(verification.Verifications.Select(item => item.EvidenceReference))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var authorizationRequest = new ArtifactResultAuthorizationRequest(
            Guid.NewGuid(), request.PublicationId, request.Identity.TenantId, request.Identity.SubjectId,
            request.Purpose, request.Environment, request.MaximumClassification,
            publication, verification, verifiedEvidence, publication.PublishedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
        ValidateAuthorization(authorizationRequest, authorization);
        var allEvidence = verifiedEvidence.Concat(authorization.EvidenceReferences)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var record = new ArtifactEvidenceRecord(
            request.PublicationId, request.CiCdExecutionId, request.DeliveryRunId,
            request.Identity.TenantId, policy.DecisionRequestId, manifest,
            publication, verification, allEvidence, authorization.DecidedAt);
        var evidence = await evidenceRecorder.RecordAsync(record, cancellationToken);
        ValidateEvidence(record, evidence);
        return new GovernedArtifactPublicationReceipt(
            request.PublicationId, request.CiCdExecutionId, request.DeliveryRunId,
            request.Identity.TenantId, policy.Outcome, true, true, true, false, false, false, false,
            request.Coordinate, request.ExpectedContentSha256Digest, publication.ImmutableRegistryReference,
            true, verification.Verifications, evidence.EvidenceReference,
            allEvidence.Append(evidence.EvidenceReference).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved Deployment", evidence.RecordedAt);
    }

    private static void ValidatePolicy(ArtifactPolicyInput input, GovernedIdentity identity, ArtifactPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.PublicationId != input.PublicationId ||
            value.CiCdExecutionId != input.CiCdExecutionId || value.DeliveryRunId != input.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, input.TenantId) || !StringComparer.Ordinal.Equals(value.Environment, input.Environment) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.PipelineManifestSha256Digest, input.PipelineManifestSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.SourceCommitId, input.SourceCommitId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.WorkflowSha256Digest, input.WorkflowSha256Digest) || value.Coordinate != input.Coordinate ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ContentSha256Digest, input.ContentSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.RegistryId, input.RegistryId) || !StringComparer.Ordinal.Equals(value.RegistryRepository, input.RegistryRepository) ||
            !StringComparer.Ordinal.Equals(value.SigningPolicyReference, input.SigningPolicyReference) || !value.AllowedControls.SetEquals(input.RequiredControls) ||
            !StringComparer.Ordinal.Equals(value.BundleId, input.PolicyBundle.BundleId) || !StringComparer.Ordinal.Equals(value.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Artifact decision.");
        if (!value.PolicySignatureValid || value.OverwriteAllowed || value.DeploymentAllowed ||
            string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) || value.MaximumClassification > input.MaximumClassification ||
            value.MaximumClassification > identity.Clearance || value.Reasons.IsDefaultOrEmpty ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("Artifact OPA decision is invalid.");
    }

    private static void ValidateCiCd(GovernedArtifactPublicationRequest request, GovernedCiCdExecutionReceipt value)
    {
        if (value.ExecutionId != request.CiCdExecutionId || value.DeliveryRunId != request.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CommitId, request.ExpectedSourceCommitId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.WorkflowSha256Digest, request.ExpectedWorkflowSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.PipelineOutputManifestSha256Digest, request.ExpectedPipelineManifestSha256Digest) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted || !value.CiCdTriggered ||
            value.SourceMutationOccurred || value.ArtifactPublished || value.DeploymentOccurred || value.ProductionEffectOccurred || value.CanAdvance ||
            string.IsNullOrWhiteSpace(value.CiCdEvidenceReference) || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Governed CI/CD prerequisite is invalid or mismatched.");
    }

    private static void ValidateManifest(GovernedArtifactPublicationRequest request, GovernedPipelineOutputManifest value)
    {
        GovernedAiPlanningRequest.ValidateDigest(value.ManifestSha256Digest, "pipeline output manifest");
        if (!StringComparer.OrdinalIgnoreCase.Equals(value.ManifestSha256Digest, request.ExpectedPipelineManifestSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CommitId, request.ExpectedSourceCommitId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.WorkflowSha256Digest, request.ExpectedWorkflowSha256Digest) ||
            !value.OutputSha256Digests.Contains(request.ExpectedContentSha256Digest, StringComparer.OrdinalIgnoreCase) ||
            value.IsReleasedArtifact || string.IsNullOrWhiteSpace(value.SbomReference) || string.IsNullOrWhiteSpace(value.ChecksumsReference) ||
            string.IsNullOrWhiteSpace(value.ProvenanceReference) || string.IsNullOrWhiteSpace(value.BuildAttestationReference) ||
            string.IsNullOrWhiteSpace(value.SignatureReference) || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Pipeline-output manifest is invalid, partial, or mismatched.");
    }

    private static void ValidateRun(GovernedArtifactPublicationRequest request, SoftwareDeliveryRun run)
    {
        run.Validate();
        var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.CiCd).ToArray();
        if (run.Id != request.DeliveryRunId || !StringComparer.Ordinal.Equals(run.TenantId, request.Identity.TenantId) ||
            run.CurrentStage != DeliveryStage.CiCd || run.History.Length != expected.Length ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) || index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run is not stopped at a valid CI/CD boundary.");
    }

    private static void ValidatePackage(ArtifactPackageValidationRequest request, ArtifactPackageValidationDecision value)
    {
        if (value.PublicationId != request.PublicationId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.PipelineManifestSha256Digest, request.Manifest.ManifestSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ContentSha256Digest, request.ExpectedContentSha256Digest) || !value.IsAllowed ||
            value.PartialOutputDetected || value.DigestSubstitutionDetected || value.ProvenanceMismatchDetected || value.SbomMismatchDetected ||
            value.AttestationMismatchDetected || value.SignatureMismatchDetected || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Institutional Artifact package validation denied or mismatched.");
    }

    private static void ValidatePublication(InstitutionalArtifactPublicationRequest request, InstitutionalArtifactPublicationResult value)
    {
        if (value.PublicationId != request.PublicationId || !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            value.Coordinate != request.Coordinate || !StringComparer.OrdinalIgnoreCase.Equals(value.ContentSha256Digest, request.ContentSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.RegistryId, request.RegistryId) || !StringComparer.Ordinal.Equals(value.RegistryRepository, request.RegistryRepository) ||
            string.IsNullOrWhiteSpace(value.ImmutableRegistryReference) || !value.Published || !value.RegistryMutated || !value.ImmutableCoordinate ||
            value.ExistingCoordinateOverwritten || !value.IdempotencyVerified || !value.ArtifactSigned || string.IsNullOrWhiteSpace(value.SignatureReference) ||
            value.SourceMutationOccurred || value.DeploymentOccurred || value.ProductionEffectOccurred ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.PublishedAt < request.RequestedAt)
            throw new InvalidOperationException("Institutional Artifact registry returned an invalid or unsafe publication proof.");
    }

    private static void ValidateAuthorization(ArtifactResultAuthorizationRequest request, ArtifactResultAuthorizationDecision value)
    {
        if (value.AuthorizationRequestId != request.AuthorizationRequestId || value.PublicationId != request.PublicationId ||
            !StringComparer.Ordinal.Equals(value.TenantId, request.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ContentSha256Digest, request.Publication.ContentSha256Digest) ||
            !value.IsAllowed || string.IsNullOrWhiteSpace(value.Code) || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Artifact result authorization denied or mismatched.");
    }

    private static void ValidateEvidence(ArtifactEvidenceRecord record, ArtifactEvidenceReceipt value)
    {
        if (value.PublicationId != record.PublicationId || value.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(value.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.ContentSha256Digest, record.Publication.ContentSha256Digest) ||
            !StringComparer.Ordinal.Equals(value.ImmutableRegistryReference, record.Publication.ImmutableRegistryReference) ||
            string.IsNullOrWhiteSpace(value.EvidenceReference) || value.RecordedAt < record.AuthorizedAt)
            throw new InvalidOperationException("Artifact evidence receipt is invalid.");
    }
}
