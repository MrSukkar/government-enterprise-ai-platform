using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Platform.Domain.Security;
using Platform.Evidence.Chain;
using Platform.Governance.Policies;

namespace Platform.SoftwareFactory.InternalService;

public enum DeploymentRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed class DeploymentRuntimeOptions
{
    public const string SectionName = "Platform:SoftwareFactory:DeploymentRuntime";
    public string Endpoint { get; init; } = string.Empty;
    public string GatewayProfile { get; init; } = string.Empty;
    public string OperatorId { get; init; } = string.Empty;
    public Guid DeploymentProfileId { get; init; }
    public string DeploymentProfileVersion { get; init; } = string.Empty;
    public string DeploymentProfileSha256Digest { get; init; } = string.Empty;
    public string TargetEnvironment { get; init; } = string.Empty;
    public string SignatureAlgorithm { get; init; } = string.Empty;
    public Dictionary<string, string> TrustedPublicKeysPem { get; init; } = new(StringComparer.Ordinal);
    public int RequestTimeoutSeconds { get; init; }
    public int MaximumRequestBytes { get; init; }
    public int MaximumResponseBytes { get; init; }

    public DeploymentRuntimeConfigurationState ConfigurationState
    {
        get
        {
            if (new[] { Endpoint, GatewayProfile, OperatorId, DeploymentProfileVersion,
                    DeploymentProfileSha256Digest, TargetEnvironment, SignatureAlgorithm }.All(string.IsNullOrWhiteSpace) &&
                DeploymentProfileId == Guid.Empty && TrustedPublicKeysPem.Count == 0 &&
                RequestTimeoutSeconds == 0 && MaximumRequestBytes == 0 && MaximumResponseBytes == 0)
                return DeploymentRuntimeConfigurationState.Unconfigured;
            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
                new[] { GatewayProfile, OperatorId, DeploymentProfileVersion, TargetEnvironment }.Any(string.IsNullOrWhiteSpace) ||
                DeploymentProfileId == Guid.Empty || !IsDigest(DeploymentProfileSha256Digest) ||
                SignatureAlgorithm is not ("RS256" or "ES256") || TrustedPublicKeysPem.Count == 0 ||
                RequestTimeoutSeconds <= 0 || MaximumRequestBytes <= 0 || MaximumResponseBytes <= 0)
                return DeploymentRuntimeConfigurationState.Invalid;
            return DeploymentRuntimeConfigurationState.Configured;
        }
    }

    public bool IsOperationallyConfigured => ConfigurationState == DeploymentRuntimeConfigurationState.Configured;
    private static bool IsDigest(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
}

public sealed record DeploymentRuntimeReadiness(DeploymentRuntimeConfigurationState State);
public sealed class DeploymentDependencyUnavailableException(string message) : Exception(message);

public sealed class SovereignDeploymentPolicyGate(
    IPolicyBundleVerifier bundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : IDeploymentPolicyGate
{
    public async Task<DeploymentPolicyDecision> EvaluateAsync(
        DeploymentPolicyInput input, CancellationToken cancellationToken)
    {
        var verified = await bundleVerifier.VerifyAsync(new(
            input.PolicyBundle.BundleId, input.PolicyBundle.Version, input.PolicyBundle.Sha256Digest,
            input.PolicyBundle.SignatureReference, input.PolicyBundle.Environment,
            input.PolicyBundle.ActivatedAt), cancellationToken);
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["deploymentId"] = input.DeploymentId.ToString("D"),
            ["artifactPublicationId"] = input.ArtifactPublicationId.ToString("D"),
            ["deliveryRunId"] = input.DeliveryRunId.ToString("D"),
            ["artifactContentSha256Digest"] = input.ArtifactContentSha256Digest,
            ["immutableRegistryReference"] = input.ImmutableRegistryReference,
            ["artifactEvidenceReference"] = input.ArtifactEvidenceReference,
            ["deploymentProfileId"] = input.DeploymentProfileId.ToString("D"),
            ["deploymentProfileVersion"] = input.DeploymentProfileVersion,
            ["deploymentProfileSha256Digest"] = input.DeploymentProfileSha256Digest,
            ["targetEnvironment"] = input.TargetEnvironment,
            ["productionDeploymentRequested"] = input.ProductionDeploymentRequested.ToString(),
            ["humanApprovalReference"] = input.HumanApprovalReference,
            ["workloadIdentityReference"] = input.WorkloadIdentityReference,
            ["secretsPolicyReference"] = input.SecretsPolicyReference,
            ["rollbackPolicyReference"] = input.RollbackPolicyReference
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences.Append(verified.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(new(
            input.DecisionRequestId, "internal-service.deployment.execute", input.ArtifactPublicationId.ToString("D"),
            input.TenantId, input.SubjectId, input.Purpose, input.MaximumClassification.ToString(),
            input.Environment, verified, attributes, evidence,
            verified.VerifiedAt > input.EvaluatedAt ? verified.VerifiedAt : input.EvaluatedAt), cancellationToken);
        if (decision.Outcome == OpaDecisionOutcome.Deny)
        {
            if (decision.Scope is not null)
                throw new UnauthorizedAccessException("Denied Deployment decision returned scope.");
            return Create(input, verified, decision, GovernedIntentPolicyOutcome.Deny,
                input.MaximumClassification, input.ProductionDeploymentRequested, false, false);
        }
        var envelope = decision.Scope ?? throw new UnauthorizedAccessException("Deployment scope missing.");
        var scope = envelope.Deployment ?? throw new UnauthorizedAccessException("Deployment action scope missing.");
        if (envelope.Artifact is not null || envelope.CiCd is not null || envelope.Git is not null ||
            envelope.HumanReview is not null || envelope.Tests is not null || envelope.Sandbox is not null ||
            envelope.SecurityValidation is not null || envelope.StaticValidation is not null ||
            envelope.CodeGeneration is not null || envelope.AiPlanning is not null ||
            envelope.ApprovedPackages is not null || envelope.ExistingArchitecture is not null ||
            envelope.ExistingSystems is not null)
            throw new UnauthorizedAccessException("Deployment decision mixed scopes.");
        if (!Enum.TryParse<DataClassification>(scope.MaximumClassification, true, out var classification) ||
            !StringComparer.OrdinalIgnoreCase.Equals(scope.ArtifactContentSha256Digest, input.ArtifactContentSha256Digest) ||
            scope.ImmutableRegistryReference != input.ImmutableRegistryReference ||
            scope.DeploymentProfileId != input.DeploymentProfileId ||
            scope.DeploymentProfileVersion != input.DeploymentProfileVersion ||
            !StringComparer.OrdinalIgnoreCase.Equals(scope.DeploymentProfileSha256Digest, input.DeploymentProfileSha256Digest) ||
            scope.TargetEnvironment != input.TargetEnvironment ||
            scope.ProductionDeploymentAllowed != input.ProductionDeploymentRequested ||
            scope.HumanApprovalReference != input.HumanApprovalReference ||
            scope.WorkloadIdentityReference != input.WorkloadIdentityReference ||
            scope.SecretsPolicyReference != input.SecretsPolicyReference ||
            scope.RollbackPolicyReference != input.RollbackPolicyReference || !scope.HumanApprovalValid ||
            scope.AiAuthorityDetected || scope.RequiredRoles.IsDefaultOrEmpty ||
            scope.OutputKind != "deployment-result")
            throw new UnauthorizedAccessException("Deployment scope mismatch.");
        return Create(input, verified, decision, GovernedIntentPolicyOutcome.Permit,
            classification, scope.ProductionDeploymentAllowed, scope.HumanApprovalValid, scope.AiAuthorityDetected);
    }

    private static DeploymentPolicyDecision Create(
        DeploymentPolicyInput input, PolicyBundleVerification verified, SovereignPolicyEvaluationDecision decision,
        GovernedIntentPolicyOutcome outcome, DataClassification classification, bool productionAllowed,
        bool humanApprovalValid, bool aiAuthorityDetected) => new(
            input.DecisionRequestId, input.DeploymentId, input.ArtifactPublicationId, input.DeliveryRunId,
            input.TenantId, input.Environment, input.ArtifactContentSha256Digest,
            input.ImmutableRegistryReference, input.DeploymentProfileId, input.DeploymentProfileVersion,
            input.DeploymentProfileSha256Digest, input.TargetEnvironment, productionAllowed,
            input.HumanApprovalReference, input.WorkloadIdentityReference, input.SecretsPolicyReference,
            input.RollbackPolicyReference, humanApprovalValid, aiAuthorityDetected,
            verified.BundleId, verified.Version, verified.Sha256Digest, true,
            verified.VerificationEvidenceReference, outcome, classification, decision.Reasons,
            decision.EvidenceReferences, decision.DecidedAt);
}

public sealed class SovereignHttpDeploymentGateway(
    IHttpClientFactory clientFactory,
    IOptions<DeploymentRuntimeOptions> configured) :
    IInstitutionalDeploymentPreflightValidator, IInstitutionalSovereignDeploymentGateway
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<DeploymentPreflightDecision> ValidateAsync(
        DeploymentPreflightRequest request, CancellationToken cancellationToken)
    {
        ValidatePinned(request.Profile, request.TargetEnvironment);
        return InvokeAsync<DeploymentPreflightDecision>("preflight", request, cancellationToken);
    }

    public Task<InstitutionalDeploymentExecutionResult> DeployAsync(
        InstitutionalDeploymentExecutionRequest request, CancellationToken cancellationToken)
    {
        ValidatePinned(request.Profile, request.TargetEnvironment);
        return InvokeAsync<InstitutionalDeploymentExecutionResult>("deploy", request, cancellationToken);
    }

    private void ValidatePinned(GovernedSovereignDeploymentProfile profile, string targetEnvironment)
    {
        var options = configured.Value;
        if (profile.ProfileId != options.DeploymentProfileId ||
            profile.Version != options.DeploymentProfileVersion ||
            !StringComparer.OrdinalIgnoreCase.Equals(profile.Sha256Digest, options.DeploymentProfileSha256Digest) ||
            targetEnvironment != options.TargetEnvironment)
            throw new UnauthorizedAccessException("Deployment profile or target is not deployment-pinned.");
    }

    private async Task<T> InvokeAsync<T>(string action, object request, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new DeploymentDependencyUnavailableException("Deployment gateway is not configured.");
        var invocationId = Guid.NewGuid();
        var inputDigest = Hash($"{invocationId:D}|{action}|{options.OperatorId}|{options.GatewayProfile}|{Hash(JsonSerializer.Serialize(request, Json))}");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new GatewayRequest(
            invocationId, action, options.OperatorId, options.GatewayProfile, inputDigest, request), Json);
        if (bytes.Length > options.MaximumRequestBytes)
            throw new UnauthorizedAccessException("Deployment gateway request exceeds its configured bound.");
        using var message = new HttpRequestMessage(HttpMethod.Post, options.Endpoint) { Content = new ByteArrayContent(bytes) };
        message.Headers.Add("X-Governed-Operation", action);
        message.Content.Headers.ContentType = new("application/json");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
        try
        {
            using var response = await clientFactory.CreateClient("sovereign-institutional-deployment")
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new DeploymentDependencyUnavailableException("Deployment gateway rejected the request.");
            var body = await SovereignAiHttp.ReadBoundedAsync(response.Content, options.MaximumResponseBytes, timeout.Token);
            var envelope = JsonSerializer.Deserialize<GatewayResponse<T>>(body, Json)
                ?? throw new InvalidOperationException("Deployment gateway returned an empty response.");
            if (envelope.InvocationId != invocationId || envelope.Action != action ||
                envelope.OperatorId != options.OperatorId || envelope.GatewayProfile != options.GatewayProfile ||
                !StringComparer.OrdinalIgnoreCase.Equals(envelope.InputSha256Digest, inputDigest) ||
                envelope.Payload is null || string.IsNullOrWhiteSpace(envelope.EvidenceReference) ||
                envelope.CompletedAt == default || envelope.Signature.SignedAt < envelope.CompletedAt)
                throw new UnauthorizedAccessException("Deployment gateway response binding is invalid.");
            var signed = $"{envelope.InvocationId:D}|{envelope.Action}|{envelope.OperatorId}|{envelope.GatewayProfile}|{envelope.InputSha256Digest}|{Hash(JsonSerializer.Serialize(envelope.Payload, Json))}|{envelope.EvidenceReference}|{envelope.CompletedAt:O}|{envelope.Signature.Algorithm}|{envelope.Signature.KeyId}|{envelope.Signature.CertificateChainReference}|{envelope.Signature.SignedAt:O}";
            if (!AiPlanningSignatureVerifier.Verify(options.SignatureAlgorithm, options.TrustedPublicKeysPem,
                    envelope.Signature, SHA256.HashData(Encoding.UTF8.GetBytes(signed))))
                throw new UnauthorizedAccessException("Deployment gateway response signature is invalid.");
            return envelope.Payload;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DeploymentDependencyUnavailableException("Deployment gateway timed out.");
        }
        catch (HttpRequestException)
        {
            throw new DeploymentDependencyUnavailableException("Deployment gateway is unavailable.");
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

public sealed class DeterministicDeploymentResultAuthorizer : IDeploymentResultAuthorizer
{
    public Task<DeploymentResultAuthorizationDecision> AuthorizeAsync(
        DeploymentResultAuthorizationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.Result.DeploymentOccurred || !request.Result.ExternalEffectOccurred ||
            !request.Result.ActivationVerified || !request.Result.IdempotencyVerified ||
            string.IsNullOrWhiteSpace(request.Result.RuntimeIdentity) ||
            string.IsNullOrWhiteSpace(request.Result.RollbackReference) || request.Result.TelemetryConfigured ||
            request.Result.AutomaticRegistrationOccurred || request.Result.EnterpriseModelMutated)
            throw new UnauthorizedAccessException("Unsafe Deployment result.");
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{request.AuthorizationRequestId:D}|{request.Result.ArtifactContentSha256Digest}|{request.Result.RuntimeIdentity}|{request.SubjectId}")));
        return Task.FromResult(new DeploymentResultAuthorizationDecision(
            request.AuthorizationRequestId, request.DeploymentId, request.TenantId,
            request.Result.ArtifactContentSha256Digest, true, "deployment-result-authorized",
            request.EvidenceReferences.Append(
                $"evidence://deployment/result-authorization/{request.AuthorizationRequestId:D}/sha256/{digest}").ToImmutableArray(),
            request.RequestedAt));
    }
}
