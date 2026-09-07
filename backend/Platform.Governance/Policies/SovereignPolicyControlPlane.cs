using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Platform.Governance.Policies;

public sealed class SovereignPolicyBundleVerifier(
    HttpClient httpClient,
    IOptions<PolicyControlPlaneOptions> configuredOptions) : IPolicyBundleVerifier
{
    public async Task<PolicyBundleVerification> VerifyAsync(
        SignedPolicyBundleReference policyBundle,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(policyBundle);
        policyBundle.Validate();
        var options = configuredOptions.Value;
        DemandConfigured(options);
        if (!StringComparer.Ordinal.Equals(policyBundle.Environment, options.Environment))
            throw new UnauthorizedAccessException("Policy bundle environment is not authorized by the control plane.");

        var request = new PolicyBundleVerificationGatewayRequest(
            policyBundle.BundleId, policyBundle.Version, policyBundle.Sha256Digest,
            policyBundle.SignatureReference, policyBundle.Environment,
            options.TrustAnchorReference, policyBundle.ActivatedAt);
        var result = await PostAsync<PolicyBundleVerificationGatewayRequest, PolicyBundleVerificationGatewayResponse>(
            httpClient, new Uri(options.BundleVerificationEndpoint), request, options, cancellationToken);
        if (!StringComparer.Ordinal.Equals(result.BundleId, policyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(result.Version, policyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(result.Sha256Digest, policyBundle.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(result.Environment, policyBundle.Environment) ||
            !result.SignatureValid || string.IsNullOrWhiteSpace(result.VerificationEvidenceReference) ||
            result.VerifiedAt < policyBundle.ActivatedAt)
            throw new UnauthorizedAccessException("Signed policy bundle verification failed closed.");
        return new PolicyBundleVerification(result.BundleId, result.Version, result.Sha256Digest,
            result.Environment, true, result.VerificationEvidenceReference, result.VerifiedAt);
    }

    internal static void DemandConfigured(PolicyControlPlaneOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.IsOperationallyConfigured)
            throw new InvalidOperationException("The sovereign policy control plane is not validly configured.");
    }

    internal static async Task<TResponse> PostAsync<TRequest, TResponse>(
        HttpClient client, Uri endpoint, TRequest request, PolicyControlPlaneOptions options,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        { Content = JsonContent.Create(request, options: JsonSerializerOptions.Web) };
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        }
        catch (HttpRequestException exception)
        {
            throw new UnauthorizedAccessException("The sovereign policy control plane is unavailable.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new UnauthorizedAccessException("The sovereign policy control-plane request timed out.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new UnauthorizedAccessException("The sovereign policy control plane denied or failed the request.");
            if (response.Content.Headers.ContentLength is long length && length > options.MaximumResponseBytes)
                throw new InvalidOperationException("Policy control-plane response exceeded its configured safety bound.");
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType is null || (!mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) &&
                !mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Policy control-plane response is not JSON.");
            var bytes = await response.Content.ReadAsByteArrayAsync(timeout.Token);
            if (bytes.Length == 0 || bytes.Length > options.MaximumResponseBytes)
                throw new InvalidOperationException("Policy control-plane response is empty or exceeds its configured safety bound.");
            return JsonSerializer.Deserialize<TResponse>(bytes, JsonSerializerOptions.Web)
                ?? throw new InvalidOperationException("Policy control-plane response is malformed.");
        }
    }

    private sealed record PolicyBundleVerificationGatewayRequest(
        string BundleId, string Version, string Sha256Digest, string SignatureReference,
        string Environment, string TrustAnchorReference, DateTimeOffset ActivatedAt);
    private sealed record PolicyBundleVerificationGatewayResponse(
        string BundleId, string Version, string Sha256Digest, string Environment,
        bool SignatureValid, string VerificationEvidenceReference, DateTimeOffset VerifiedAt);
}

public sealed class SovereignOpaPolicyDecisionPoint(
    HttpClient httpClient,
    IOptions<PolicyControlPlaneOptions> configuredOptions) : IOpaPolicyDecisionPoint
{
    public async Task<OpaPolicyDecision> EvaluateAsync(OpaPolicyInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.Action.Validate();
        var options = configuredOptions.Value;
        SovereignPolicyBundleVerifier.DemandConfigured(options);
        if (!input.VerifiedPolicyBundle.SignatureValid ||
            !StringComparer.Ordinal.Equals(input.Action.PolicyBundle.BundleId, input.VerifiedPolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(input.Action.PolicyBundle.Version, input.VerifiedPolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(input.Action.PolicyBundle.Sha256Digest, input.VerifiedPolicyBundle.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(input.Action.Environment, options.Environment) ||
            input.EvaluatedAt < input.VerifiedPolicyBundle.VerifiedAt)
            throw new UnauthorizedAccessException("OPA input is not bound to a verified active policy bundle.");

        var result = await SovereignPolicyBundleVerifier.PostAsync<OpaPolicyInput, OpaPolicyGatewayResponse>(
            httpClient, new Uri(options.OpaEndpoint), input, options, cancellationToken);
        if (result.DecisionRequestId != input.DecisionRequestId ||
            !StringComparer.Ordinal.Equals(result.BundleId, input.VerifiedPolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(result.BundleVersion, input.VerifiedPolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(result.BundleSha256Digest, input.VerifiedPolicyBundle.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(result.Environment, input.Action.Environment) ||
            !Enum.IsDefined(result.Outcome) || result.Reasons.IsDefaultOrEmpty ||
            result.EvidenceReferences.IsDefaultOrEmpty || result.DecidedAt < input.EvaluatedAt)
            throw new UnauthorizedAccessException("OPA returned an invalid or mismatched decision.");
        foreach (var value in result.Reasons.Concat(result.EvidenceReferences))
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new OpaPolicyDecision(result.DecisionRequestId, result.BundleId, result.BundleVersion,
            result.BundleSha256Digest, result.Environment, result.Outcome,
            result.Reasons.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
            result.EvidenceReferences.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
            result.DecidedAt);
    }

    private sealed record OpaPolicyGatewayResponse(
        Guid DecisionRequestId, string BundleId, string BundleVersion, string BundleSha256Digest,
        string Environment, OpaDecisionOutcome Outcome, ImmutableArray<string> Reasons,
        ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);
}
