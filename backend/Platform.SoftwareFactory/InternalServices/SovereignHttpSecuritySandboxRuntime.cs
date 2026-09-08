using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Platform.Evidence.Chain;
using Platform.SoftwareFactory.Packages;
using Platform.SoftwareFactory.Sandbox;

namespace Platform.SoftwareFactory.InternalService;

public sealed class SovereignHttpSecuritySandboxRuntime(
    IHttpClientFactory httpClientFactory,
    IOptions<SandboxRuntimeOptions> configuredOptions) : ISecuritySandboxRuntime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SandboxExecutionResult> ExecuteAsync(
        SandboxExecutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Run.Validate(); request.Candidate.Validate(); request.SandboxImage.Validate(); request.IsolationPolicy.Validate();
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured || request.Run.CurrentStage != Delivery.DeliveryStage.SecurityValidation ||
            request.SandboxImage.Kind != PackageKind.SandboxImage || !request.SandboxImageDecision.IsAllowed)
            throw new SandboxDependencyUnavailableException("Sovereign Sandbox runtime is not validly configured.");

        var invocationId = Guid.NewGuid();
        var candidateDigest = SovereignHttpCodeGenerationRuntime.CandidateDigest(request.Candidate);
        var inputDigest = InputDigest(invocationId, options, request, candidateDigest);
        var wireRequest = new SandboxInvocationRequest(invocationId, options.OperatorId, options.RuntimeProfile,
            inputDigest, request.Run.Id, request.Run.TenantId, candidateDigest, request.Candidate,
            request.SandboxImage, request.IsolationPolicy, request.NonSecretEnvironmentReferences,
            ProductionCredentialsAllowed: false, HostFilesystemAccessAllowed: false,
            NetworkDefaultDenyRequired: true);
        var requestBytes = JsonSerializer.SerializeToUtf8Bytes(wireRequest, JsonOptions);
        if (requestBytes.Length > options.MaximumRequestBytes)
            throw new UnauthorizedAccessException("Authorized Sandbox request exceeds its deployment bound.");
        using var message = new HttpRequestMessage(HttpMethod.Post, options.Endpoint)
            { Content = new ByteArrayContent(requestBytes) };
        message.Content.Headers.ContentType = new("application/json");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
        try
        {
            var client = httpClientFactory.CreateClient("sovereign-security-sandbox");
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new SandboxDependencyUnavailableException("Sovereign Sandbox runtime rejected the bounded request.");
            var bytes = await SovereignAiHttp.ReadBoundedAsync(response.Content, options.MaximumResponseBytes, timeout.Token);
            var result = JsonSerializer.Deserialize<SandboxInvocationResponse>(bytes, JsonOptions)
                ?? throw new InvalidOperationException("Sovereign Sandbox runtime returned an empty response.");
            Validate(result, invocationId, inputDigest, candidateDigest, request, options);
            return new SandboxExecutionResult(result.ExitCode, result.TimedOut,
                result.IsolationViolationDetected, result.ProducedArtifactReferences,
                result.ExecutionEvidenceReference);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new SandboxDependencyUnavailableException("Sovereign Sandbox runtime timed out."); }
        catch (HttpRequestException)
        { throw new SandboxDependencyUnavailableException("Sovereign Sandbox runtime is unavailable."); }
        catch (JsonException exception)
        { throw new InvalidOperationException("Sovereign Sandbox runtime response is malformed.", exception); }
    }

    private static void Validate(SandboxInvocationResponse value, Guid invocationId, string inputDigest,
        string candidateDigest, SandboxExecutionRequest request, SandboxRuntimeOptions options)
    {
        var produced = value.ProducedArtifactReferences;
        if (value.InvocationId != invocationId || !StringComparer.Ordinal.Equals(value.OperatorId, options.OperatorId) ||
            !StringComparer.Ordinal.Equals(value.RuntimeProfile, options.RuntimeProfile) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.InputSha256Digest, inputDigest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, candidateDigest) ||
            value.SandboxImage != request.SandboxImage ||
            !StringComparer.Ordinal.Equals(value.IsolationClass, request.IsolationPolicy.IsolationClass) ||
            value.Ephemeral != request.IsolationPolicy.Ephemeral || value.MicroVmIsolation != request.IsolationPolicy.MicroVmIsolation ||
            value.ProductionCredentialsMounted || value.HostFilesystemMounted || !value.NetworkDefaultDenyEnforced ||
            !value.AllowedNetworkDestinations.ToImmutableHashSet(StringComparer.Ordinal)
                .SetEquals(request.IsolationPolicy.AllowedNetworkDestinations) ||
            value.ExitCode != 0 || value.TimedOut || value.IsolationViolationDetected || produced.IsDefault ||
            produced.Any(string.IsNullOrWhiteSpace) || produced.Distinct(StringComparer.Ordinal).Count() != produced.Length ||
            string.IsNullOrWhiteSpace(value.ExecutionEvidenceReference) ||
            !value.ExecutionEvidenceReference.StartsWith("evidence://", StringComparison.Ordinal) ||
            value.CompletedAt == default || value.Signature.SignedAt < value.CompletedAt)
            throw new UnauthorizedAccessException("Sovereign Sandbox response failed exact isolation and result validation.");
        var payload = ResponsePayload(value);
        if (!AiPlanningSignatureVerifier.Verify(options.SignatureAlgorithm, options.TrustedPublicKeysPem,
                value.Signature, SHA256.HashData(Encoding.UTF8.GetBytes(payload))))
            throw new UnauthorizedAccessException("Sovereign Sandbox response signature is invalid.");
    }

    private static string InputDigest(Guid invocationId, SandboxRuntimeOptions options,
        SandboxExecutionRequest request, string candidateDigest)
    {
        var policy = request.IsolationPolicy;
        var canonical = new StringBuilder().Append(invocationId.ToString("D")).Append('|')
            .Append(options.OperatorId).Append('|').Append(options.RuntimeProfile).Append('|')
            .Append(request.Run.Id.ToString("D")).Append('|').Append(request.Run.TenantId).Append('|')
            .Append(candidateDigest).Append('|').Append(CoordinateKey(request.SandboxImage)).Append('|')
            .Append(policy.IsolationClass).Append('|').Append(policy.Ephemeral).Append('|')
            .Append(policy.MicroVmIsolation).Append('|').Append(policy.ProductionCredentialsAllowed).Append('|')
            .Append(policy.HostFilesystemAccessAllowed).Append('|').Append(policy.NetworkDefaultDeny).Append('|')
            .Append(policy.CpuLimit).Append('|').Append(policy.MemoryLimitBytes).Append('|')
            .Append(policy.ExecutionTimeout.Ticks).Append('|')
            .AppendJoin(',', policy.AllowedNetworkDestinations.Order(StringComparer.Ordinal)).Append('|')
            .AppendJoin(',', request.NonSecretEnvironmentReferences.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => $"{item.Key}={item.Value}"));
        return Sha256(canonical.ToString());
    }

    private static string ResponsePayload(SandboxInvocationResponse value) => string.Join('|',
        value.InvocationId.ToString("D"), value.OperatorId, value.RuntimeProfile,
        value.InputSha256Digest.ToLowerInvariant(), value.CandidateSha256Digest.ToLowerInvariant(),
        CoordinateKey(value.SandboxImage), value.IsolationClass, value.Ephemeral,
        value.MicroVmIsolation, value.ProductionCredentialsMounted, value.HostFilesystemMounted,
        value.NetworkDefaultDenyEnforced,
        string.Join(',', value.AllowedNetworkDestinations.Order(StringComparer.Ordinal)), value.ExitCode,
        value.TimedOut, value.IsolationViolationDetected,
        string.Join(',', value.ProducedArtifactReferences.Order(StringComparer.Ordinal)),
        value.ExecutionEvidenceReference, value.CompletedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        value.Signature.Algorithm, value.Signature.KeyId, value.Signature.CertificateChainReference,
        value.Signature.SignedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

    private static string CoordinateKey(PackageCoordinate value) =>
        $"{value.Kind}|{value.Name}|{value.Version}|{value.ContentDigest}";
    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record SandboxInvocationRequest(
        Guid InvocationId, string OperatorId, string RuntimeProfile, string InputSha256Digest,
        Guid DeliveryRunId, string TenantId, string CandidateSha256Digest,
        AiDevelopment.AiCandidateArtifact Candidate, PackageCoordinate SandboxImage,
        SandboxIsolationPolicy IsolationPolicy, ImmutableDictionary<string, string> NonSecretEnvironmentReferences,
        bool ProductionCredentialsAllowed, bool HostFilesystemAccessAllowed, bool NetworkDefaultDenyRequired);

    private sealed record SandboxInvocationResponse(
        Guid InvocationId, string OperatorId, string RuntimeProfile, string InputSha256Digest,
        string CandidateSha256Digest, PackageCoordinate SandboxImage, string IsolationClass,
        bool Ephemeral, bool MicroVmIsolation, bool ProductionCredentialsMounted,
        bool HostFilesystemMounted, bool NetworkDefaultDenyEnforced,
        ImmutableArray<string> AllowedNetworkDestinations, int ExitCode, bool TimedOut,
        bool IsolationViolationDetected, ImmutableArray<string> ProducedArtifactReferences,
        string ExecutionEvidenceReference, DateTimeOffset CompletedAt, SignatureEnvelope Signature);
}
