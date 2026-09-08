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

public sealed class SovereignHttpAiOutputEvaluator(
    IHttpClientFactory httpClientFactory,
    IOptions<AiPlanningRuntimeOptions> configuredOptions) : IAiOutputEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AiEvaluationReport> EvaluateAsync(
        AiDevelopmentRequest request, AiCandidateArtifact candidate, CancellationToken cancellationToken)
    {
        request.Validate(); candidate.Validate(); var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured || request.TaskKind != AiDevelopmentTaskKind.Planning ||
            StringComparer.Ordinal.Equals(options.GenerationOperatorId, options.EvaluationOperatorId) ||
            StringComparer.Ordinal.Equals(options.GenerationRuntimeProfile, options.EvaluationRuntimeProfile))
            throw new AiPlanningDependencyUnavailableException("Independent sovereign AI evaluation is not validly configured.");
        var evaluationId = Guid.NewGuid();
        var candidateDigest = SovereignHttpAiPlanningRuntime.CandidateDigest(candidate);
        var inputDigest = EvaluationInputDigest(evaluationId, request, candidateDigest, options);
        var wireRequest = new EvaluationRequest(evaluationId, options.EvaluationOperatorId,
            options.EvaluationRuntimeProfile, inputDigest, candidateDigest, request.Purpose,
            request.PromptTemplateId, request.AuthorizedContextItems.Select(item => item.Sha256Digest)
                .Order(StringComparer.Ordinal).ToImmutableArray(), request.ApprovedPackages,
            request.Constraints, candidate, ToolsEnabled: false);
        var requestBytes = JsonSerializer.SerializeToUtf8Bytes(wireRequest, JsonOptions);
        if (requestBytes.Length > options.MaximumRequestBytes)
            throw new UnauthorizedAccessException("Independent evaluation request exceeds its deployment bound.");
        using var message = new HttpRequestMessage(HttpMethod.Post, options.EvaluationEndpoint)
        { Content = new ByteArrayContent(requestBytes) };
        message.Content.Headers.ContentType = new("application/json");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
        try
        {
            var client = httpClientFactory.CreateClient("sovereign-ai-planning-evaluation");
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new AiPlanningDependencyUnavailableException("Independent sovereign AI evaluation rejected the bounded request.");
            var bytes = await SovereignAiHttp.ReadBoundedAsync(response.Content, options.MaximumResponseBytes, timeout.Token);
            var result = JsonSerializer.Deserialize<EvaluationResponse>(bytes, JsonOptions)
                ?? throw new InvalidOperationException("Independent sovereign AI evaluation returned an empty response.");
            Validate(result, evaluationId, inputDigest, candidateDigest, options);
            return new(result.EvaluatorId, result.IsIndependentFromGenerationRuntime,
                result.Findings, result.EvaluatedAt);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new AiPlanningDependencyUnavailableException("Independent sovereign AI evaluation timed out."); }
        catch (HttpRequestException)
        { throw new AiPlanningDependencyUnavailableException("Independent sovereign AI evaluation is unavailable."); }
        catch (JsonException exception)
        { throw new InvalidOperationException("Independent sovereign AI evaluation response is malformed.", exception); }
    }

    private static void Validate(EvaluationResponse value, Guid evaluationId, string inputDigest,
        string candidateDigest, AiPlanningRuntimeOptions options)
    {
        var required = Enum.GetValues<AiEvaluationCriterion>();
        if (value.EvaluationId != evaluationId || !StringComparer.Ordinal.Equals(value.EvaluatorId, options.EvaluationOperatorId) ||
            !StringComparer.Ordinal.Equals(value.RuntimeProfile, options.EvaluationRuntimeProfile) ||
            StringComparer.Ordinal.Equals(value.EvaluatorId, options.GenerationOperatorId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.InputSha256Digest, inputDigest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(value.CandidateSha256Digest, candidateDigest) ||
            !value.IsIndependentFromGenerationRuntime || value.Findings.Length != required.Length ||
            value.Findings.Select(item => item.Criterion).Distinct().Count() != required.Length ||
            value.Findings.Any(item => !item.Passed || string.IsNullOrWhiteSpace(item.Rationale) ||
                string.IsNullOrWhiteSpace(item.EvidenceReference)) || value.EvaluatedAt == default ||
            value.Signature.SignedAt < value.EvaluatedAt)
            throw new UnauthorizedAccessException("Independent sovereign AI evaluation failed exact binding validation.");
        var payload = string.Join("|", value.EvaluationId.ToString("D"), value.EvaluatorId,
            value.RuntimeProfile, value.InputSha256Digest.ToLowerInvariant(), value.CandidateSha256Digest.ToLowerInvariant(),
            value.IsIndependentFromGenerationRuntime.ToString(CultureInfo.InvariantCulture),
            string.Join(',', value.Findings.OrderBy(item => item.Criterion)
                .Select(item => $"{item.Criterion}:{item.Passed}:{item.Rationale}:{item.EvidenceReference}")),
            value.EvaluatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            value.Signature.Algorithm, value.Signature.KeyId, value.Signature.CertificateChainReference,
            value.Signature.SignedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        if (!AiPlanningSignatureVerifier.Verify(options.SignatureAlgorithm,
                options.EvaluationTrustedPublicKeysPem, value.Signature,
                SHA256.HashData(Encoding.UTF8.GetBytes(payload))))
            throw new UnauthorizedAccessException("Independent sovereign AI evaluation signature is invalid.");
    }

    private static string EvaluationInputDigest(Guid evaluationId, AiDevelopmentRequest request,
        string candidateDigest, AiPlanningRuntimeOptions options) => SovereignHttpAiPlanningRuntime.Sha256(string.Join("|",
            evaluationId.ToString("D"), options.EvaluationOperatorId, options.EvaluationRuntimeProfile,
            request.Run.Id.ToString("D"), request.Purpose, request.PromptTemplateId, candidateDigest,
            string.Join(',', request.AuthorizedContextItems.Select(item => item.Sha256Digest).Order(StringComparer.Ordinal)),
            string.Join(',', request.Constraints.Order(StringComparer.Ordinal)), "tools:false"));

    private sealed record EvaluationRequest(
        Guid EvaluationId, string EvaluatorId, string RuntimeProfile, string InputSha256Digest,
        string CandidateSha256Digest, string Purpose, string PromptTemplateId,
        ImmutableArray<string> ContextSha256Digests,
        ImmutableArray<PackageCoordinate> ApprovedPackages,
        ImmutableArray<string> Constraints, AiCandidateArtifact Candidate, bool ToolsEnabled);

    private sealed record EvaluationResponse(
        Guid EvaluationId, string EvaluatorId, string RuntimeProfile, string InputSha256Digest,
        string CandidateSha256Digest, bool IsIndependentFromGenerationRuntime,
        ImmutableArray<AiEvaluationFinding> Findings, DateTimeOffset EvaluatedAt,
        SignatureEnvelope Signature);
}
