using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Platform.Domain.Security;
using Platform.Evidence.Chain;
using Platform.Governance.Policies;
using Platform.SoftwareFactory.SupplyChain;

namespace Platform.SoftwareFactory.InternalService;

public enum ArtifactRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed class ArtifactRuntimeOptions
{
    public const string SectionName = "Platform:SoftwareFactory:ArtifactRuntime";
    public string Endpoint { get; init; } = string.Empty;
    public string GatewayProfile { get; init; } = string.Empty;
    public string OperatorId { get; init; } = string.Empty;
    public string RegistryId { get; init; } = string.Empty;
    public string RegistryRepository { get; init; } = string.Empty;
    public string SigningPolicyReference { get; init; } = string.Empty;
    public string SignatureAlgorithm { get; init; } = string.Empty;
    public Dictionary<string, string> TrustedPublicKeysPem { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> VerifierIdentities { get; init; } = new(StringComparer.Ordinal);
    public int RequestTimeoutSeconds { get; init; }
    public int MaximumRequestBytes { get; init; }
    public int MaximumResponseBytes { get; init; }

    public ArtifactRuntimeConfigurationState ConfigurationState
    {
        get
        {
            if (new[] { Endpoint, GatewayProfile, OperatorId, RegistryId, RegistryRepository,
                    SigningPolicyReference, SignatureAlgorithm }.All(string.IsNullOrWhiteSpace) &&
                TrustedPublicKeysPem.Count == 0 && VerifierIdentities.Count == 0 &&
                RequestTimeoutSeconds == 0 && MaximumRequestBytes == 0 && MaximumResponseBytes == 0)
                return ArtifactRuntimeConfigurationState.Unconfigured;
            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
                new[] { GatewayProfile, OperatorId, RegistryId, RegistryRepository, SigningPolicyReference }.Any(string.IsNullOrWhiteSpace) ||
                SignatureAlgorithm is not ("RS256" or "ES256") || TrustedPublicKeysPem.Count == 0 ||
                RequestTimeoutSeconds <= 0 || MaximumRequestBytes <= 0 || MaximumResponseBytes <= 0)
                return ArtifactRuntimeConfigurationState.Invalid;
            var required = Enum.GetNames<SupplyChainControl>().ToImmutableHashSet(StringComparer.Ordinal);
            return VerifierIdentities.Keys.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(required) &&
                   VerifierIdentities.Values.All(value => !string.IsNullOrWhiteSpace(value))
                ? ArtifactRuntimeConfigurationState.Configured
                : ArtifactRuntimeConfigurationState.Invalid;
        }
    }

    public bool IsOperationallyConfigured => ConfigurationState == ArtifactRuntimeConfigurationState.Configured;
}

public sealed record ArtifactRuntimeReadiness(ArtifactRuntimeConfigurationState State);
public sealed class ArtifactDependencyUnavailableException(string message) : Exception(message);

public sealed class SovereignArtifactPolicyGate(
    IPolicyBundleVerifier bundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : IArtifactPolicyGate
{
    public async Task<ArtifactPolicyDecision> EvaluateAsync(ArtifactPolicyInput input, CancellationToken cancellationToken)
    {
        var verified = await bundleVerifier.VerifyAsync(new(
            input.PolicyBundle.BundleId, input.PolicyBundle.Version, input.PolicyBundle.Sha256Digest,
            input.PolicyBundle.SignatureReference, input.PolicyBundle.Environment,
            input.PolicyBundle.ActivatedAt), cancellationToken);
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["publicationId"] = input.PublicationId.ToString("D"),
            ["ciCdExecutionId"] = input.CiCdExecutionId.ToString("D"),
            ["deliveryRunId"] = input.DeliveryRunId.ToString("D"),
            ["pipelineManifestSha256Digest"] = input.PipelineManifestSha256Digest,
            ["sourceCommitId"] = input.SourceCommitId,
            ["workflowSha256Digest"] = input.WorkflowSha256Digest,
            ["artifactName"] = input.Coordinate.Name,
            ["artifactVersion"] = input.Coordinate.Version,
            ["artifactKind"] = input.Coordinate.Kind,
            ["contentSha256Digest"] = input.ContentSha256Digest,
            ["registryId"] = input.RegistryId,
            ["registryRepository"] = input.RegistryRepository,
            ["signingPolicyReference"] = input.SigningPolicyReference,
            ["requiredControls"] = string.Join(',', input.RequiredControls.Order())
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences.Append(verified.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(new(
            input.DecisionRequestId, "internal-service.artifact.publish", input.Coordinate.ToString(),
            input.TenantId, input.SubjectId, input.Purpose, input.MaximumClassification.ToString(),
            input.Environment, verified, attributes, evidence,
            verified.VerifiedAt > input.EvaluatedAt ? verified.VerifiedAt : input.EvaluatedAt), cancellationToken);
        if (decision.Outcome == OpaDecisionOutcome.Deny)
        {
            if (decision.Scope is not null)
                throw new UnauthorizedAccessException("Denied Artifact decision returned scope.");
            return Create(input, verified, decision, GovernedIntentPolicyOutcome.Deny,
                input.MaximumClassification, input.RequiredControls, false, false);
        }

        var envelope = decision.Scope ?? throw new UnauthorizedAccessException("Artifact scope missing.");
        var scope = envelope.Artifact ?? throw new UnauthorizedAccessException("Artifact action scope missing.");
        if (envelope.CiCd is not null || envelope.Git is not null || envelope.HumanReview is not null ||
            envelope.Tests is not null || envelope.Sandbox is not null || envelope.SecurityValidation is not null ||
            envelope.StaticValidation is not null || envelope.CodeGeneration is not null || envelope.AiPlanning is not null ||
            envelope.ApprovedPackages is not null || envelope.ExistingArchitecture is not null || envelope.ExistingSystems is not null)
            throw new UnauthorizedAccessException("Artifact decision mixed scopes.");
        if (!Enum.TryParse<DataClassification>(scope.MaximumClassification, true, out var classification) ||
            !StringComparer.OrdinalIgnoreCase.Equals(scope.PipelineManifestSha256Digest, input.PipelineManifestSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(scope.SourceCommitId, input.SourceCommitId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(scope.WorkflowSha256Digest, input.WorkflowSha256Digest) ||
            scope.ArtifactName != input.Coordinate.Name || scope.ArtifactVersion != input.Coordinate.Version ||
            scope.ArtifactKind != input.Coordinate.Kind ||
            !StringComparer.OrdinalIgnoreCase.Equals(scope.ContentSha256Digest, input.ContentSha256Digest) ||
            scope.RegistryId != input.RegistryId || scope.RegistryRepository != input.RegistryRepository ||
            scope.SigningPolicyReference != input.SigningPolicyReference ||
            !scope.AllowedControls.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(input.RequiredControls.Select(value => value.ToString())) ||
            scope.OverwriteAllowed || scope.DeploymentAllowed || scope.RequiredRoles.IsDefaultOrEmpty ||
            scope.OutputKind != "artifact-publication-result")
            throw new UnauthorizedAccessException("Artifact scope mismatch.");
        return Create(input, verified, decision, GovernedIntentPolicyOutcome.Permit, classification,
            input.RequiredControls, scope.OverwriteAllowed, scope.DeploymentAllowed);
    }

    private static ArtifactPolicyDecision Create(
        ArtifactPolicyInput input, PolicyBundleVerification verified, SovereignPolicyEvaluationDecision decision,
        GovernedIntentPolicyOutcome outcome, DataClassification classification,
        ImmutableHashSet<SupplyChainControl> controls, bool overwrite, bool deployment) => new(
            input.DecisionRequestId, input.PublicationId, input.CiCdExecutionId, input.DeliveryRunId,
            input.TenantId, input.Environment, input.PipelineManifestSha256Digest, input.SourceCommitId,
            input.WorkflowSha256Digest, input.Coordinate, input.ContentSha256Digest, input.RegistryId,
            input.RegistryRepository, input.SigningPolicyReference, controls, overwrite, deployment,
            verified.BundleId, verified.Version, verified.Sha256Digest, true,
            verified.VerificationEvidenceReference, outcome, classification, decision.Reasons,
            decision.EvidenceReferences, decision.DecidedAt);
}

public sealed class SovereignHttpArtifactGateway(
    IHttpClientFactory clientFactory,
    IOptions<ArtifactRuntimeOptions> configured) : IArtifactPackageValidator, IInstitutionalArtifactRegistryGateway
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<ArtifactPackageValidationDecision> ValidateAsync(
        ArtifactPackageValidationRequest request, CancellationToken cancellationToken) =>
        InvokeAsync<ArtifactPackageValidationDecision>("validate-package", request, cancellationToken);

    public Task<InstitutionalArtifactPublicationResult> PublishAsync(
        InstitutionalArtifactPublicationRequest request, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (request.RegistryId != options.RegistryId || request.RegistryRepository != options.RegistryRepository ||
            request.SigningPolicyReference != options.SigningPolicyReference)
            throw new UnauthorizedAccessException("Artifact publication target is not deployment-pinned.");
        return InvokeAsync<InstitutionalArtifactPublicationResult>("publish", request, cancellationToken);
    }

    internal async Task<SupplyChainVerification> VerifyAsync(
        SupplyChainControl control, ArtifactSupplyChainRecord artifact, CancellationToken cancellationToken)
    {
        var result = await InvokeAsync<SupplyChainVerification>($"verify-{control}", artifact, cancellationToken);
        var options = configured.Value;
        if (result.Control != control || !options.VerifierIdentities.TryGetValue(control.ToString(), out var identity) ||
            result.VerifierIdentity != identity || !result.Passed || string.IsNullOrWhiteSpace(result.EvidenceReference))
            throw new UnauthorizedAccessException("Artifact supply-chain verification is invalid.");
        return result;
    }

    private async Task<T> InvokeAsync<T>(string action, object request, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new ArtifactDependencyUnavailableException("Artifact gateway is not configured.");
        var invocationId = Guid.NewGuid();
        var requestDigest = Hash($"{invocationId:D}|{action}|{options.OperatorId}|{options.GatewayProfile}|{Hash(JsonSerializer.Serialize(request, Json))}");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new GatewayRequest(
            invocationId, action, options.OperatorId, options.GatewayProfile, requestDigest, request), Json);
        if (bytes.Length > options.MaximumRequestBytes)
            throw new UnauthorizedAccessException("Artifact gateway request exceeds its configured bound.");
        using var message = new HttpRequestMessage(HttpMethod.Post, options.Endpoint) { Content = new ByteArrayContent(bytes) };
        message.Headers.Add("X-Governed-Operation", action);
        message.Content.Headers.ContentType = new("application/json");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
        try
        {
            using var response = await clientFactory.CreateClient("sovereign-institutional-artifact")
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new ArtifactDependencyUnavailableException("Artifact gateway rejected the request.");
            var body = await SovereignAiHttp.ReadBoundedAsync(response.Content, options.MaximumResponseBytes, timeout.Token);
            var envelope = JsonSerializer.Deserialize<GatewayResponse<T>>(body, Json)
                ?? throw new InvalidOperationException("Artifact gateway returned an empty response.");
            if (envelope.InvocationId != invocationId || envelope.Action != action ||
                envelope.OperatorId != options.OperatorId || envelope.GatewayProfile != options.GatewayProfile ||
                !StringComparer.OrdinalIgnoreCase.Equals(envelope.InputSha256Digest, requestDigest) ||
                envelope.Payload is null || string.IsNullOrWhiteSpace(envelope.EvidenceReference) ||
                envelope.CompletedAt == default || envelope.Signature.SignedAt < envelope.CompletedAt)
                throw new UnauthorizedAccessException("Artifact gateway response binding is invalid.");
            var signed = $"{envelope.InvocationId:D}|{envelope.Action}|{envelope.OperatorId}|{envelope.GatewayProfile}|{envelope.InputSha256Digest}|{Hash(JsonSerializer.Serialize(envelope.Payload, Json))}|{envelope.EvidenceReference}|{envelope.CompletedAt:O}|{envelope.Signature.Algorithm}|{envelope.Signature.KeyId}|{envelope.Signature.CertificateChainReference}|{envelope.Signature.SignedAt:O}";
            if (!AiPlanningSignatureVerifier.Verify(options.SignatureAlgorithm, options.TrustedPublicKeysPem,
                    envelope.Signature, SHA256.HashData(Encoding.UTF8.GetBytes(signed))))
                throw new UnauthorizedAccessException("Artifact gateway response signature is invalid.");
            return envelope.Payload;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ArtifactDependencyUnavailableException("Artifact gateway timed out.");
        }
        catch (HttpRequestException)
        {
            throw new ArtifactDependencyUnavailableException("Artifact gateway is unavailable.");
        }
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record GatewayRequest(Guid InvocationId, string Action, string OperatorId,
        string GatewayProfile, string InputSha256Digest, object Payload);
    private sealed record GatewayResponse<T>(Guid InvocationId, string Action, string OperatorId,
        string GatewayProfile, string InputSha256Digest, T Payload, string EvidenceReference,
        DateTimeOffset CompletedAt, SignatureEnvelope Signature);
}

public sealed class InstitutionalArtifactSupplyChainVerifier(
    SupplyChainControl control,
    SovereignHttpArtifactGateway gateway) : ISupplyChainControlVerifier
{
    public SupplyChainControl Control { get; } = control;
    public Task<SupplyChainVerification> VerifyAsync(ArtifactSupplyChainRecord artifact, CancellationToken cancellationToken) =>
        gateway.VerifyAsync(Control, artifact, cancellationToken);
}

public sealed class DeterministicArtifactResultAuthorizer : IArtifactResultAuthorizer
{
    public Task<ArtifactResultAuthorizationDecision> AuthorizeAsync(
        ArtifactResultAuthorizationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.Publication.Published || !request.Publication.RegistryMutated ||
            !request.Publication.ImmutableCoordinate || request.Publication.ExistingCoordinateOverwritten ||
            !request.Publication.IdempotencyVerified || !request.Publication.ArtifactSigned ||
            request.Publication.SourceMutationOccurred || request.Publication.DeploymentOccurred ||
            request.Publication.ProductionEffectOccurred || !request.Verification.IsVerified)
            throw new UnauthorizedAccessException("Unsafe Artifact publication result.");
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{request.AuthorizationRequestId:D}|{request.Publication.ContentSha256Digest}|{request.SubjectId}")));
        return Task.FromResult(new ArtifactResultAuthorizationDecision(
            request.AuthorizationRequestId, request.PublicationId, request.TenantId,
            request.Publication.ContentSha256Digest, true, "artifact-result-authorized",
            request.EvidenceReferences.Append(
                $"evidence://artifact/result-authorization/{request.AuthorizationRequestId:D}/sha256/{digest}").ToImmutableArray(),
            request.RequestedAt));
    }
}
