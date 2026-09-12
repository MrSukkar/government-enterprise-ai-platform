using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Platform.Domain.Security;
using Platform.Evidence.Chain;
using Platform.Governance.Policies;

namespace Platform.SoftwareFactory.InternalService;

public enum OpenTelemetryRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed class OpenTelemetryRuntimeOptions
{
    public const string SectionName = "Platform:SoftwareFactory:OpenTelemetryRuntime";
    public string Endpoint { get; init; } = string.Empty;
    public string GatewayProfile { get; init; } = string.Empty;
    public string OperatorId { get; init; } = string.Empty;
    public Guid TelemetryProfileId { get; init; }
    public string TelemetryProfileVersion { get; init; } = string.Empty;
    public string TelemetryProfileSha256Digest { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string ServiceVersion { get; init; } = string.Empty;
    public string CollectorAgentEndpoint { get; init; } = string.Empty;
    public string CollectorGatewayEndpoint { get; init; } = string.Empty;
    public string TrustAnchorReference { get; init; } = string.Empty;
    public string RedactionPolicyReference { get; init; } = string.Empty;
    public string RedactionPolicySha256Digest { get; init; } = string.Empty;
    public string SignatureAlgorithm { get; init; } = string.Empty;
    public Dictionary<string, string> TrustedPublicKeysPem { get; init; } = new(StringComparer.Ordinal);
    public int RequestTimeoutSeconds { get; init; }
    public int MaximumRequestBytes { get; init; }
    public int MaximumResponseBytes { get; init; }

    public OpenTelemetryRuntimeConfigurationState ConfigurationState
    {
        get
        {
            var values = new[] { Endpoint, GatewayProfile, OperatorId, TelemetryProfileVersion,
                TelemetryProfileSha256Digest, ServiceName, ServiceVersion, CollectorAgentEndpoint,
                CollectorGatewayEndpoint, TrustAnchorReference, RedactionPolicyReference,
                RedactionPolicySha256Digest, SignatureAlgorithm };
            if (values.All(string.IsNullOrWhiteSpace) && TelemetryProfileId == Guid.Empty &&
                TrustedPublicKeysPem.Count == 0 && RequestTimeoutSeconds == 0 &&
                MaximumRequestBytes == 0 && MaximumResponseBytes == 0)
                return OpenTelemetryRuntimeConfigurationState.Unconfigured;
            if (!SafeHttps(Endpoint) || !SafeHttps(CollectorAgentEndpoint) || !SafeHttps(CollectorGatewayEndpoint) ||
                new[] { GatewayProfile, OperatorId, TelemetryProfileVersion, ServiceName, ServiceVersion,
                    TrustAnchorReference, RedactionPolicyReference }.Any(string.IsNullOrWhiteSpace) ||
                TelemetryProfileId == Guid.Empty || !IsDigest(TelemetryProfileSha256Digest) ||
                !IsDigest(RedactionPolicySha256Digest) || SignatureAlgorithm is not ("RS256" or "ES256") ||
                TrustedPublicKeysPem.Count == 0 || RequestTimeoutSeconds <= 0 ||
                MaximumRequestBytes <= 0 || MaximumResponseBytes <= 0)
                return OpenTelemetryRuntimeConfigurationState.Invalid;
            return OpenTelemetryRuntimeConfigurationState.Configured;
        }
    }

    public bool IsOperationallyConfigured => ConfigurationState == OpenTelemetryRuntimeConfigurationState.Configured;
    private static bool IsDigest(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
    private static bool SafeHttps(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) &&
        string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
}

public sealed record OpenTelemetryRuntimeReadiness(OpenTelemetryRuntimeConfigurationState State);
public sealed class OpenTelemetryDependencyUnavailableException(string message) : Exception(message);

public sealed class SovereignOpenTelemetryPolicyGate(
    IPolicyBundleVerifier bundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : IOpenTelemetryPolicyGate
{
    public async Task<OpenTelemetryPolicyDecision> EvaluateAsync(
        OpenTelemetryPolicyInput input, CancellationToken cancellationToken)
    {
        var verified = await bundleVerifier.VerifyAsync(new(
            input.PolicyBundle.BundleId, input.PolicyBundle.Version, input.PolicyBundle.Sha256Digest,
            input.PolicyBundle.SignatureReference, input.PolicyBundle.Environment,
            input.PolicyBundle.ActivatedAt), cancellationToken);
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["activationId"] = input.ActivationId.ToString("D"),
            ["deploymentId"] = input.DeploymentId.ToString("D"),
            ["deliveryRunId"] = input.DeliveryRunId.ToString("D"),
            ["runtimeIdentity"] = input.RuntimeIdentity,
            ["artifactContentSha256Digest"] = input.ArtifactContentSha256Digest,
            ["deploymentEvidenceReference"] = input.DeploymentEvidenceReference,
            ["productionEffectOccurred"] = input.ProductionEffectOccurred.ToString(),
            ["telemetryProfileId"] = input.TelemetryProfileId.ToString("D"),
            ["telemetryProfileVersion"] = input.TelemetryProfileVersion,
            ["telemetryProfileSha256Digest"] = input.TelemetryProfileSha256Digest,
            ["serviceName"] = input.ServiceName,
            ["serviceVersion"] = input.ServiceVersion,
            ["requiredSignals"] = string.Join(',', input.RequiredSignals.Order()),
            ["redactionPolicyReference"] = input.RedactionPolicyReference,
            ["redactionPolicySha256Digest"] = input.RedactionPolicySha256Digest
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences.Append(verified.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(new(
            input.DecisionRequestId, "internal-service.opentelemetry.activate", input.DeploymentId.ToString("D"),
            input.TenantId, input.SubjectId, input.Purpose, input.MaximumClassification.ToString(),
            input.Environment, verified, attributes, evidence,
            verified.VerifiedAt > input.EvaluatedAt ? verified.VerifiedAt : input.EvaluatedAt), cancellationToken);
        if (decision.Outcome == OpaDecisionOutcome.Deny)
        {
            if (decision.Scope is not null)
                throw new UnauthorizedAccessException("Denied OpenTelemetry decision returned scope.");
            return Create(input, verified, decision, GovernedIntentPolicyOutcome.Deny,
                input.MaximumClassification, input.RequiredSignals, false, false);
        }
        var envelope = decision.Scope ?? throw new UnauthorizedAccessException("OpenTelemetry scope missing.");
        var scope = envelope.OpenTelemetry ?? throw new UnauthorizedAccessException("OpenTelemetry action scope missing.");
        if (envelope.Deployment is not null || envelope.Artifact is not null || envelope.CiCd is not null ||
            envelope.Git is not null || envelope.HumanReview is not null || envelope.Tests is not null ||
            envelope.Sandbox is not null || envelope.SecurityValidation is not null ||
            envelope.StaticValidation is not null || envelope.CodeGeneration is not null ||
            envelope.AiPlanning is not null || envelope.ApprovedPackages is not null ||
            envelope.ExistingArchitecture is not null || envelope.ExistingSystems is not null)
            throw new UnauthorizedAccessException("OpenTelemetry decision mixed scopes.");
        if (!Enum.TryParse<DataClassification>(scope.MaximumClassification, true, out var classification) ||
            scope.RuntimeIdentity != input.RuntimeIdentity ||
            !StringComparer.OrdinalIgnoreCase.Equals(scope.ArtifactContentSha256Digest, input.ArtifactContentSha256Digest) ||
            scope.ProductionEffectOccurred != input.ProductionEffectOccurred ||
            scope.TelemetryProfileId != input.TelemetryProfileId ||
            scope.TelemetryProfileVersion != input.TelemetryProfileVersion ||
            !StringComparer.OrdinalIgnoreCase.Equals(scope.TelemetryProfileSha256Digest, input.TelemetryProfileSha256Digest) ||
            scope.ServiceName != input.ServiceName || scope.ServiceVersion != input.ServiceVersion ||
            !scope.AllowedSignals.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(input.RequiredSignals.Select(value => value.ToString())) ||
            scope.RedactionPolicyReference != input.RedactionPolicyReference ||
            !StringComparer.OrdinalIgnoreCase.Equals(scope.RedactionPolicySha256Digest, input.RedactionPolicySha256Digest) ||
            scope.RegistrationAllowed || scope.EnterpriseModelMutationAllowed ||
            scope.RequiredRoles.IsDefaultOrEmpty || scope.OutputKind != "opentelemetry-result")
            throw new UnauthorizedAccessException("OpenTelemetry scope mismatch.");
        return Create(input, verified, decision, GovernedIntentPolicyOutcome.Permit,
            classification, input.RequiredSignals, scope.RegistrationAllowed, scope.EnterpriseModelMutationAllowed);
    }

    private static OpenTelemetryPolicyDecision Create(
        OpenTelemetryPolicyInput input, PolicyBundleVerification verified,
        SovereignPolicyEvaluationDecision decision, GovernedIntentPolicyOutcome outcome,
        DataClassification classification, ImmutableHashSet<GovernedTelemetrySignal> signals,
        bool registrationAllowed, bool enterpriseModelMutationAllowed) => new(
            input.DecisionRequestId, input.ActivationId, input.DeploymentId, input.DeliveryRunId,
            input.TenantId, input.Environment, input.RuntimeIdentity, input.ArtifactContentSha256Digest,
            input.ProductionEffectOccurred, input.TelemetryProfileId, input.TelemetryProfileVersion,
            input.TelemetryProfileSha256Digest, input.ServiceName, input.ServiceVersion, signals,
            input.RedactionPolicyReference, input.RedactionPolicySha256Digest, registrationAllowed,
            enterpriseModelMutationAllowed, verified.BundleId, verified.Version, verified.Sha256Digest,
            true, verified.VerificationEvidenceReference, outcome, classification,
            decision.Reasons, decision.EvidenceReferences, decision.DecidedAt);
}

public sealed class SovereignHttpOpenTelemetryGateway(
    IHttpClientFactory clientFactory,
    IOptions<OpenTelemetryRuntimeOptions> configured) :
    IOpenTelemetryRedactionPolicyVerifier, IInstitutionalOpenTelemetryGateway
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<OpenTelemetryRedactionVerificationDecision> VerifyAsync(
        OpenTelemetryRedactionVerificationRequest request, CancellationToken cancellationToken)
    {
        ValidatePinned(request.Profile);
        return InvokeAsync<OpenTelemetryRedactionVerificationDecision>("verify-redaction", request, cancellationToken);
    }

    public Task<InstitutionalOpenTelemetryActivationResult> ActivateAsync(
        InstitutionalOpenTelemetryActivationRequest request, CancellationToken cancellationToken)
    {
        ValidatePinned(request.Profile);
        return InvokeAsync<InstitutionalOpenTelemetryActivationResult>("activate", request, cancellationToken);
    }

    private void ValidatePinned(GovernedOpenTelemetryProfile profile)
    {
        var options = configured.Value;
        if (profile.ProfileId != options.TelemetryProfileId || profile.Version != options.TelemetryProfileVersion ||
            !StringComparer.OrdinalIgnoreCase.Equals(profile.Sha256Digest, options.TelemetryProfileSha256Digest) ||
            profile.ServiceName != options.ServiceName || profile.ServiceVersion != options.ServiceVersion ||
            profile.CollectorAgentEndpoint.AbsoluteUri != options.CollectorAgentEndpoint ||
            profile.CollectorGatewayEndpoint.AbsoluteUri != options.CollectorGatewayEndpoint ||
            profile.TrustAnchorReference != options.TrustAnchorReference ||
            profile.RedactionPolicyReference != options.RedactionPolicyReference ||
            !StringComparer.OrdinalIgnoreCase.Equals(profile.RedactionPolicySha256Digest, options.RedactionPolicySha256Digest))
            throw new UnauthorizedAccessException("OpenTelemetry profile is not deployment-pinned.");
    }

    private async Task<T> InvokeAsync<T>(string action, object request, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new OpenTelemetryDependencyUnavailableException("OpenTelemetry gateway is not configured.");
        var invocationId = Guid.NewGuid();
        var inputDigest = Hash($"{invocationId:D}|{action}|{options.OperatorId}|{options.GatewayProfile}|{Hash(JsonSerializer.Serialize(request, Json))}");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new GatewayRequest(
            invocationId, action, options.OperatorId, options.GatewayProfile, inputDigest, request), Json);
        if (bytes.Length > options.MaximumRequestBytes)
            throw new UnauthorizedAccessException("OpenTelemetry gateway request exceeds its configured bound.");
        using var message = new HttpRequestMessage(HttpMethod.Post, options.Endpoint) { Content = new ByteArrayContent(bytes) };
        message.Headers.Add("X-Governed-Operation", action);
        message.Content.Headers.ContentType = new("application/json");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
        try
        {
            using var response = await clientFactory.CreateClient("sovereign-institutional-opentelemetry")
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new OpenTelemetryDependencyUnavailableException("OpenTelemetry gateway rejected the request.");
            var body = await SovereignAiHttp.ReadBoundedAsync(response.Content, options.MaximumResponseBytes, timeout.Token);
            var envelope = JsonSerializer.Deserialize<GatewayResponse<T>>(body, Json)
                ?? throw new InvalidOperationException("OpenTelemetry gateway returned an empty response.");
            if (envelope.InvocationId != invocationId || envelope.Action != action ||
                envelope.OperatorId != options.OperatorId || envelope.GatewayProfile != options.GatewayProfile ||
                !StringComparer.OrdinalIgnoreCase.Equals(envelope.InputSha256Digest, inputDigest) ||
                envelope.Payload is null || string.IsNullOrWhiteSpace(envelope.EvidenceReference) ||
                envelope.CompletedAt == default || envelope.Signature.SignedAt < envelope.CompletedAt)
                throw new UnauthorizedAccessException("OpenTelemetry gateway response binding is invalid.");
            var signed = $"{envelope.InvocationId:D}|{envelope.Action}|{envelope.OperatorId}|{envelope.GatewayProfile}|{envelope.InputSha256Digest}|{Hash(JsonSerializer.Serialize(envelope.Payload, Json))}|{envelope.EvidenceReference}|{envelope.CompletedAt:O}|{envelope.Signature.Algorithm}|{envelope.Signature.KeyId}|{envelope.Signature.CertificateChainReference}|{envelope.Signature.SignedAt:O}";
            if (!AiPlanningSignatureVerifier.Verify(options.SignatureAlgorithm, options.TrustedPublicKeysPem,
                    envelope.Signature, SHA256.HashData(Encoding.UTF8.GetBytes(signed))))
                throw new UnauthorizedAccessException("OpenTelemetry gateway response signature is invalid.");
            return envelope.Payload;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OpenTelemetryDependencyUnavailableException("OpenTelemetry gateway timed out.");
        }
        catch (HttpRequestException)
        {
            throw new OpenTelemetryDependencyUnavailableException("OpenTelemetry gateway is unavailable.");
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

public sealed class DeterministicOpenTelemetryResultAuthorizer : IOpenTelemetryResultAuthorizer
{
    public Task<OpenTelemetryResultAuthorizationDecision> AuthorizeAsync(
        OpenTelemetryResultAuthorizationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var required = Enum.GetValues<GovernedTelemetrySignal>().ToImmutableHashSet();
        if (!request.Result.ConfigurationApplied || !request.Result.ResourceIdentityBound ||
            !request.Result.CollectorTrustVerified || !request.Result.TraceAwareRoutingVerified ||
            !request.Result.RedactionEnforced || !request.Result.BaggageClearedAtStart ||
            !request.Result.BaggageClearedAtEnd || !request.Result.ExternalEffectOccurred ||
            request.Result.AutomaticRegistrationOccurred || request.Result.EnterpriseModelMutated ||
            !request.Result.Signals.Select(value => value.Signal).ToImmutableHashSet().SetEquals(required) ||
            request.Result.Signals.Any(value => !value.Configured || !value.Accepted))
            throw new UnauthorizedAccessException("Unsafe OpenTelemetry result.");
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{request.AuthorizationRequestId:D}|{request.Result.TelemetryProfileSha256Digest}|{request.Result.RuntimeIdentity}|{request.SubjectId}")));
        return Task.FromResult(new OpenTelemetryResultAuthorizationDecision(
            request.AuthorizationRequestId, request.ActivationId, request.TenantId,
            request.Result.TelemetryProfileSha256Digest, true, "opentelemetry-result-authorized",
            request.EvidenceReferences.Append(
                $"evidence://opentelemetry/result-authorization/{request.AuthorizationRequestId:D}/sha256/{digest}").ToImmutableArray(),
            request.RequestedAt));
    }
}
