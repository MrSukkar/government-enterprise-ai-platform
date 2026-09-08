using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Platform.Evidence.Chain;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.InternalService;

public sealed class SovereignHttpCodeGenerationRuntime(
    IHttpClientFactory httpClientFactory,
    IOptions<CodeGenerationRuntimeOptions> configuredOptions) : ICodeGenerationAiDevelopmentRuntime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public string RuntimeProfile => configuredOptions.Value.GenerationRuntimeProfile;
    public int RequestTimeoutSeconds => configuredOptions.Value.RequestTimeoutSeconds;
    public int MaximumRequestBytes => configuredOptions.Value.MaximumRequestBytes;
    public int MaximumResponseBytes => configuredOptions.Value.MaximumResponseBytes;

    public async Task<AiCandidateArtifact> ExecuteAsync(AiDevelopmentRequest request, CancellationToken cancellationToken)
    {
        request.Validate(); var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured || request.TaskKind != AiDevelopmentTaskKind.CodeGeneration ||
            request.Run.CurrentStage != Delivery.DeliveryStage.AiPlanning ||
            !StringComparer.Ordinal.Equals(RuntimeProfile, options.GenerationRuntimeProfile) ||
            request.RequestTimeoutSeconds != options.RequestTimeoutSeconds ||
            request.MaximumRequestBytes != options.MaximumRequestBytes || request.MaximumResponseBytes != options.MaximumResponseBytes)
            throw new CodeGenerationDependencyUnavailableException("Sovereign Code Generation is not validly configured.");
        foreach (var path in request.AuthorizedOutputPaths) GovernedGeneratedPath.Validate(path);
        var invocationId = Guid.NewGuid(); var inputDigest = InputDigest(request, invocationId, options.GenerationOperatorId);
        var wireRequest = new CodeInvocationRequest(invocationId, options.GenerationOperatorId,
            options.GenerationRuntimeProfile, inputDigest, request.Purpose, request.VerifiedPromptContent!,
            request.AuthorizedContextItems, request.ApprovedPackages, request.Constraints,
            request.AuthorizedOutputPaths, ToolsEnabled: false, FilesystemWriteEnabled: false,
            CommandsEnabled: false, GeneratedFilesApplied: false);
        var requestBytes = JsonSerializer.SerializeToUtf8Bytes(wireRequest, JsonOptions);
        if (requestBytes.Length > options.MaximumRequestBytes) throw new UnauthorizedAccessException("Code Generation request exceeds its deployment bound.");
        using var message = new HttpRequestMessage(HttpMethod.Post, options.GenerationEndpoint) { Content = new ByteArrayContent(requestBytes) };
        message.Content.Headers.ContentType = new("application/json");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
        try
        {
            var client = httpClientFactory.CreateClient("sovereign-code-generation");
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode) throw new CodeGenerationDependencyUnavailableException("Sovereign Code Generation rejected the bounded request.");
            var bytes = await SovereignAiHttp.ReadBoundedAsync(response.Content, options.MaximumResponseBytes, timeout.Token);
            var result = JsonSerializer.Deserialize<CodeInvocationResponse>(bytes, JsonOptions)
                ?? throw new InvalidOperationException("Sovereign Code Generation returned an empty response.");
            Validate(result, invocationId, inputDigest, request, options);
            return new(result.InvocationId, result.RuntimeProfile, result.Content,
                result.ContextReferences, result.GeneratedFilePaths, result.CreatedAt,
                result.EvidenceReferences.Order(StringComparer.Ordinal).ToImmutableArray());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new CodeGenerationDependencyUnavailableException("Sovereign Code Generation timed out."); }
        catch (HttpRequestException) { throw new CodeGenerationDependencyUnavailableException("Sovereign Code Generation is unavailable."); }
        catch (JsonException exception) { throw new InvalidOperationException("Sovereign Code Generation response is malformed.", exception); }
    }

    private static void Validate(CodeInvocationResponse value, Guid invocationId, string inputDigest,
        AiDevelopmentRequest request, CodeGenerationRuntimeOptions options)
    {
        foreach (var path in value.GeneratedFilePaths) GovernedGeneratedPath.Validate(path);
        var outputDigest = OutputDigest(value.Content, value.GeneratedFilePaths);
        if (value.InvocationId != invocationId || !StringComparer.Ordinal.Equals(value.OperatorId, options.GenerationOperatorId) ||
            !StringComparer.Ordinal.Equals(value.RuntimeProfile, options.GenerationRuntimeProfile) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.InputSha256Digest, inputDigest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.OutputSha256Digest, outputDigest) ||
            string.IsNullOrWhiteSpace(value.Content) || value.ToolsRequested || value.CommandsRequested || value.FilesApplied ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.GeneratedFilePaths.IsDefaultOrEmpty ||
            !value.GeneratedFilePaths.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(request.AuthorizedOutputPaths) ||
            value.GeneratedFilePaths.Distinct(StringComparer.Ordinal).Count() != value.GeneratedFilePaths.Length ||
            !value.ContextReferences.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(request.AuthorizedContextReferences) ||
            value.CreatedAt == default || value.Signature.SignedAt < value.CreatedAt)
            throw new UnauthorizedAccessException("Sovereign Code Generation response failed exact binding validation.");
        var payload = string.Join('|', value.InvocationId.ToString("D"), value.OperatorId, value.RuntimeProfile,
            value.InputSha256Digest.ToLowerInvariant(), outputDigest,
            string.Join(',', value.ContextReferences.Order(StringComparer.Ordinal)),
            string.Join(',', value.GeneratedFilePaths.Order(StringComparer.Ordinal)), value.ToolsRequested, value.CommandsRequested,
            value.FilesApplied, value.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            string.Join(',', value.EvidenceReferences.Order(StringComparer.Ordinal)), value.Signature.Algorithm,
            value.Signature.KeyId, value.Signature.CertificateChainReference,
            value.Signature.SignedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        if (!AiPlanningSignatureVerifier.Verify(options.SignatureAlgorithm, options.GenerationTrustedPublicKeysPem,
                value.Signature, SHA256.HashData(Encoding.UTF8.GetBytes(payload))))
            throw new UnauthorizedAccessException("Sovereign Code Generation signature is invalid.");
    }

    internal static string InputDigest(AiDevelopmentRequest request, Guid invocationId, string operatorId)
    {
        var canonical = new StringBuilder().Append(invocationId.ToString("D")).Append('|').Append(operatorId).Append('|')
            .Append(request.Run.Id.ToString("D")).Append('|').Append(request.TaskKind).Append('|').Append(request.Purpose)
            .Append('|').Append(request.PromptTemplateId).Append('|').Append(Sha256(request.VerifiedPromptContent!));
        foreach (var item in request.AuthorizedContextItems.OrderBy(item => item.Reference, StringComparer.Ordinal))
            canonical.Append('|').Append(item.Reference).Append(':').Append(item.Sha256Digest.ToLowerInvariant()).Append(':').Append(item.Classification);
        foreach (var package in request.ApprovedPackages.OrderBy(CoordinateKey, StringComparer.Ordinal)) canonical.Append('|').Append(CoordinateKey(package));
        foreach (var constraint in request.Constraints.Order(StringComparer.Ordinal)) canonical.Append('|').Append(constraint);
        foreach (var path in request.AuthorizedOutputPaths.Order(StringComparer.Ordinal)) canonical.Append('|').Append(path);
        canonical.Append('|').Append(request.RequestTimeoutSeconds).Append('|').Append(request.MaximumRequestBytes)
            .Append('|').Append(request.MaximumResponseBytes).Append("|tools:false|commands:false|filesystem:false|applied:false");
        return Sha256(canonical.ToString());
    }

    internal static string CandidateDigest(AiCandidateArtifact candidate) => Sha256(string.Join('|',
        candidate.InvocationId.ToString("D"), candidate.RuntimeProfile, candidate.Content,
        string.Join(',', candidate.ContextReferences.Order(StringComparer.Ordinal)),
        string.Join(',', candidate.GeneratedFilePaths.Order(StringComparer.Ordinal)),
        candidate.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        string.Join(',', candidate.EvidenceReferences.Order(StringComparer.Ordinal))));
    private static string OutputDigest(string content, ImmutableArray<string> paths) =>
        Sha256($"{content}|{string.Join(',', paths.Order(StringComparer.Ordinal))}");
    private static string CoordinateKey(PackageCoordinate value) => $"{value.Kind}|{value.Name}|{value.Version}|{value.ContentDigest}";
    internal static string Sha256(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record CodeInvocationRequest(Guid InvocationId, string OperatorId, string RuntimeProfile,
        string InputSha256Digest, string Purpose, string Prompt, ImmutableArray<AiDevelopmentContextItem> Context,
        ImmutableArray<PackageCoordinate> ApprovedPackages, ImmutableArray<string> Constraints,
        ImmutableArray<string> AuthorizedOutputPaths, bool ToolsEnabled, bool FilesystemWriteEnabled,
        bool CommandsEnabled, bool GeneratedFilesApplied);
    private sealed record CodeInvocationResponse(Guid InvocationId, string OperatorId, string RuntimeProfile,
        string InputSha256Digest, string OutputSha256Digest, string Content, ImmutableArray<string> ContextReferences,
        ImmutableArray<string> GeneratedFilePaths, bool ToolsRequested, bool CommandsRequested, bool FilesApplied,
        ImmutableArray<string> EvidenceReferences, DateTimeOffset CreatedAt, SignatureEnvelope Signature);
}
