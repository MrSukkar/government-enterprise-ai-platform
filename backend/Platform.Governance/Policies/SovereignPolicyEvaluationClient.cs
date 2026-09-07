using System.Collections.Immutable;
using Microsoft.Extensions.Options;

namespace Platform.Governance.Policies;

public sealed class SovereignPolicyEvaluationClient(
    HttpClient httpClient,
    IOptions<PolicyControlPlaneOptions> configuredOptions) : ISovereignPolicyEvaluationClient
{
    public async Task<SovereignPolicyEvaluationDecision> EvaluateAsync(
        SovereignPolicyEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        var options = configuredOptions.Value;
        SovereignPolicyBundleVerifier.DemandConfigured(options);
        if (!StringComparer.Ordinal.Equals(request.Environment, options.Environment))
            throw new UnauthorizedAccessException("Policy evaluation environment is not authorized by the control plane.");

        var result = await SovereignPolicyBundleVerifier.PostAsync<SovereignPolicyEvaluationRequest, GatewayResponse>(
            httpClient, new Uri(options.OpaEndpoint), request, options, cancellationToken);
        if (result.DecisionRequestId != request.DecisionRequestId ||
            !StringComparer.Ordinal.Equals(result.Action, request.Action) ||
            !StringComparer.Ordinal.Equals(result.ResourceId, request.ResourceId) ||
            !StringComparer.Ordinal.Equals(result.BundleId, request.VerifiedPolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(result.BundleVersion, request.VerifiedPolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(result.BundleSha256Digest, request.VerifiedPolicyBundle.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(result.Environment, request.Environment) ||
            !Enum.TryParse<OpaDecisionOutcome>(result.Outcome, ignoreCase: false, out var outcome) ||
            !Enum.IsDefined(outcome) || result.Reasons.IsDefaultOrEmpty ||
            result.EvidenceReferences.IsDefaultOrEmpty || result.DecidedAt < request.EvaluatedAt)
            throw new UnauthorizedAccessException("OPA returned an invalid or mismatched policy decision.");
        foreach (var value in result.Reasons.Concat(result.EvidenceReferences))
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new SovereignPolicyEvaluationDecision(
            result.DecisionRequestId, result.Action, result.ResourceId, result.BundleId,
            result.BundleVersion, result.BundleSha256Digest, result.Environment, outcome,
            Normalize(result.Reasons), Normalize(result.EvidenceReferences), result.Scope,
            result.DecidedAt);
    }

    private static void ValidateRequest(SovereignPolicyEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.DecisionRequestId == Guid.Empty)
            throw new InvalidOperationException("Policy decision request identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Action);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ResourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Classification);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Environment);
        ArgumentNullException.ThrowIfNull(request.VerifiedPolicyBundle);
        if (!request.VerifiedPolicyBundle.SignatureValid)
            throw new UnauthorizedAccessException("Policy evaluation requires a verified signed bundle.");
        ArgumentNullException.ThrowIfNull(request.Attributes);
        if (request.Attributes.Count == 0 || request.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Policy evaluation requires scoped attributes and evidence.");
        foreach (var item in request.Attributes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Key);
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Value);
        }
        foreach (var value in request.EvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (request.EvaluatedAt < request.VerifiedPolicyBundle.VerifiedAt)
            throw new InvalidOperationException("Policy evaluation predates signed bundle verification.");
    }

    private static ImmutableArray<string> Normalize(ImmutableArray<string> values) =>
        values.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();

    private sealed record GatewayResponse(
        Guid DecisionRequestId, string Action, string ResourceId, string BundleId,
        string BundleVersion, string BundleSha256Digest, string Environment, string Outcome,
        ImmutableArray<string> Reasons, ImmutableArray<string> EvidenceReferences,
        SovereignPolicyEvaluationScope? Scope,
        DateTimeOffset DecidedAt);
}
