using System.Collections.Immutable;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Platform.Evidence.Chain;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.InternalService;

public sealed class SovereignHttpAiPlanningRuntime(
    IHttpClientFactory httpClientFactory,
    IOptions<AiPlanningRuntimeOptions> configuredOptions) : IAiDevelopmentRuntime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public string RuntimeProfile => configuredOptions.Value.GenerationRuntimeProfile;
    public int RequestTimeoutSeconds => configuredOptions.Value.RequestTimeoutSeconds;
    public int MaximumRequestBytes => configuredOptions.Value.MaximumRequestBytes;
    public int MaximumResponseBytes => configuredOptions.Value.MaximumResponseBytes;

    public async Task<AiCandidateArtifact> ExecuteAsync(
        AiDevelopmentRequest request, CancellationToken cancellationToken)
    {
        request.Validate(); var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured || request.TaskKind != AiDevelopmentTaskKind.Planning ||
            !StringComparer.Ordinal.Equals(request.Run.CurrentStage.ToString(), "ApprovedPackages") ||
            !StringComparer.Ordinal.Equals(RuntimeProfile, options.GenerationRuntimeProfile) ||
            request.RequestTimeoutSeconds != options.RequestTimeoutSeconds ||
            request.MaximumRequestBytes != options.MaximumRequestBytes ||
            request.MaximumResponseBytes != options.MaximumResponseBytes)
            throw new AiPlanningDependencyUnavailableException("Sovereign AI Planning generation is not validly configured.");

        var invocationId = Guid.NewGuid();
        var inputDigest = InputDigest(request, invocationId, options.GenerationOperatorId);
        var wireRequest = new PlanningInvocationRequest(invocationId, options.GenerationOperatorId,
            options.GenerationRuntimeProfile, inputDigest, request.Purpose, request.VerifiedPromptContent!,
            request.AuthorizedContextItems, request.ApprovedPackages, request.Constraints,
            ToolsEnabled: false, GeneratedFilesAllowed: false);
        var requestBytes = JsonSerializer.SerializeToUtf8Bytes(wireRequest, JsonOptions);
        if (requestBytes.Length > options.MaximumRequestBytes)
            throw new UnauthorizedAccessException("Authorized AI Planning request exceeds its deployment bound.");

        using var message = new HttpRequestMessage(HttpMethod.Post, options.GenerationEndpoint)
        { Content = new ByteArrayContent(requestBytes) };
        message.Content.Headers.ContentType = new("application/json");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
        try
        {
            var client = httpClientFactory.CreateClient("sovereign-ai-planning-generation");
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new AiPlanningDependencyUnavailableException("Sovereign AI Planning generation rejected the bounded request.");
            var bytes = await SovereignAiHttp.ReadBoundedAsync(response.Content, options.MaximumResponseBytes, timeout.Token);
            var result = JsonSerializer.Deserialize<PlanningInvocationResponse>(bytes, JsonOptions)
                ?? throw new InvalidOperationException("Sovereign AI Planning generation returned an empty response.");
            Validate(result, invocationId, inputDigest, request, options);
            return new(result.InvocationId, result.RuntimeProfile, result.Content,
                result.ContextReferences, result.GeneratedFilePaths, result.CreatedAt,
                result.EvidenceReferences.Order(StringComparer.Ordinal).ToImmutableArray());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new AiPlanningDependencyUnavailableException("Sovereign AI Planning generation timed out."); }
        catch (HttpRequestException)
        { throw new AiPlanningDependencyUnavailableException("Sovereign AI Planning generation is unavailable."); }
        catch (JsonException exception)
        { throw new InvalidOperationException("Sovereign AI Planning generation response is malformed.", exception); }
    }

    private static void Validate(PlanningInvocationResponse value, Guid invocationId, string inputDigest,
        AiDevelopmentRequest request, AiPlanningRuntimeOptions options)
    {
        var outputDigest = Sha256(value.Content);
        if (value.InvocationId != invocationId || !StringComparer.Ordinal.Equals(value.OperatorId, options.GenerationOperatorId) ||
            !StringComparer.Ordinal.Equals(value.RuntimeProfile, options.GenerationRuntimeProfile) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.InputSha256Digest, inputDigest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.OutputSha256Digest, outputDigest) ||
            string.IsNullOrWhiteSpace(value.Content) || value.ToolsRequested ||
            !value.GeneratedFilePaths.IsDefaultOrEmpty || value.EvidenceReferences.IsDefaultOrEmpty ||
            !value.ContextReferences.ToImmutableHashSet(StringComparer.Ordinal)
                .SetEquals(request.AuthorizedContextReferences) || value.CreatedAt == default ||
            value.Signature.SignedAt < value.CreatedAt)
            throw new UnauthorizedAccessException("Sovereign AI Planning generation response failed exact binding validation.");
        var signedPayload = ResponsePayload(value, outputDigest);
        if (!AiPlanningSignatureVerifier.Verify(options.SignatureAlgorithm,
                options.GenerationTrustedPublicKeysPem, value.Signature,
                SHA256.HashData(Encoding.UTF8.GetBytes(signedPayload))))
            throw new UnauthorizedAccessException("Sovereign AI Planning generation signature is invalid.");
    }

    internal static string InputDigest(AiDevelopmentRequest request, Guid invocationId, string operatorId)
    {
        var canonical = new StringBuilder().Append(invocationId.ToString("D")).Append('|')
            .Append(operatorId).Append('|').Append(request.Run.Id.ToString("D")).Append('|')
            .Append(request.TaskKind).Append('|').Append(request.Purpose).Append('|')
            .Append(request.PromptTemplateId).Append('|').Append(Sha256(request.VerifiedPromptContent!));
        foreach (var item in request.AuthorizedContextItems.OrderBy(item => item.Reference, StringComparer.Ordinal))
            canonical.Append('|').Append(item.Reference).Append(':').Append(item.Sha256Digest.ToLowerInvariant())
                .Append(':').Append(item.Classification);
        foreach (var package in request.ApprovedPackages.OrderBy(CoordinateKey, StringComparer.Ordinal))
            canonical.Append('|').Append(CoordinateKey(package));
        foreach (var constraint in request.Constraints.Order(StringComparer.Ordinal)) canonical.Append('|').Append(constraint);
        canonical.Append('|').Append(request.RequestTimeoutSeconds.ToString(CultureInfo.InvariantCulture))
            .Append('|').Append(request.MaximumRequestBytes.ToString(CultureInfo.InvariantCulture))
            .Append('|').Append(request.MaximumResponseBytes.ToString(CultureInfo.InvariantCulture))
            .Append("|tools:false|generated-files:false");
        return Sha256(canonical.ToString());
    }

    private static string ResponsePayload(PlanningInvocationResponse value, string outputDigest) => string.Join("|",
        value.InvocationId.ToString("D"), value.OperatorId, value.RuntimeProfile,
        value.InputSha256Digest.ToLowerInvariant(), outputDigest,
        string.Join(',', value.ContextReferences.Order(StringComparer.Ordinal)),
        value.ToolsRequested.ToString(CultureInfo.InvariantCulture),
        value.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        string.Join(',', value.EvidenceReferences.Order(StringComparer.Ordinal)),
        value.Signature.Algorithm, value.Signature.KeyId, value.Signature.CertificateChainReference,
        value.Signature.SignedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

    internal static string CandidateDigest(AiCandidateArtifact candidate) => Sha256(string.Join("|",
        candidate.InvocationId.ToString("D"), candidate.RuntimeProfile, candidate.Content,
        string.Join(',', candidate.ContextReferences.Order(StringComparer.Ordinal)),
        candidate.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        string.Join(',', candidate.EvidenceReferences.Order(StringComparer.Ordinal))));

    private static string CoordinateKey(PackageCoordinate value) =>
        $"{value.Kind}|{value.Name}|{value.Version}|{value.ContentDigest}";
    internal static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record PlanningInvocationRequest(
        Guid InvocationId, string OperatorId, string RuntimeProfile, string InputSha256Digest,
        string Purpose, string Prompt, ImmutableArray<AiDevelopmentContextItem> Context,
        ImmutableArray<PackageCoordinate> ApprovedPackages, ImmutableArray<string> Constraints,
        bool ToolsEnabled, bool GeneratedFilesAllowed);

    private sealed record PlanningInvocationResponse(
        Guid InvocationId, string OperatorId, string RuntimeProfile, string InputSha256Digest,
        string OutputSha256Digest, string Content, ImmutableArray<string> ContextReferences,
        ImmutableArray<string> GeneratedFilePaths, bool ToolsRequested,
        ImmutableArray<string> EvidenceReferences, DateTimeOffset CreatedAt, SignatureEnvelope Signature);
}

internal static class SovereignAiHttp
{
    public static async Task<byte[]> ReadBoundedAsync(HttpContent content, int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > 0 && content.Headers.ContentLength > maximumBytes)
            throw new UnauthorizedAccessException("Sovereign AI response exceeds its deployment bound.");
        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        using var destination = new MemoryStream(); var buffer = new byte[8192];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken); if (read == 0) break;
            if (destination.Length + read > maximumBytes)
                throw new UnauthorizedAccessException("Sovereign AI response exceeds its deployment bound.");
            destination.Write(buffer, 0, read);
        }
        return destination.ToArray();
    }
}
