namespace Platform.SoftwareFactory.InternalService;

public enum AiPlanningRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed class AiPlanningRuntimeOptions
{
    public const string SectionName = "Platform:SoftwareFactory:AiPlanningRuntime";
    public string GenerationEndpoint { get; init; } = string.Empty;
    public string EvaluationEndpoint { get; init; } = string.Empty;
    public string GenerationRuntimeProfile { get; init; } = string.Empty;
    public string EvaluationRuntimeProfile { get; init; } = string.Empty;
    public string GenerationOperatorId { get; init; } = string.Empty;
    public string EvaluationOperatorId { get; init; } = string.Empty;
    public string SignatureAlgorithm { get; init; } = string.Empty;
    public Dictionary<string, string> PromptTrustedPublicKeysPem { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> GenerationTrustedPublicKeysPem { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> EvaluationTrustedPublicKeysPem { get; init; } = new(StringComparer.Ordinal);
    public int RequestTimeoutSeconds { get; init; }
    public int MaximumRequestBytes { get; init; }
    public int MaximumResponseBytes { get; init; }

    public AiPlanningRuntimeConfigurationState ConfigurationState
    {
        get
        {
            if (new[] { GenerationEndpoint, EvaluationEndpoint, GenerationRuntimeProfile,
                    EvaluationRuntimeProfile, GenerationOperatorId, EvaluationOperatorId,
                    SignatureAlgorithm }.All(string.IsNullOrWhiteSpace) &&
                PromptTrustedPublicKeysPem.Count == 0 && GenerationTrustedPublicKeysPem.Count == 0 &&
                EvaluationTrustedPublicKeysPem.Count == 0 &&
                RequestTimeoutSeconds == 0 && MaximumRequestBytes == 0 && MaximumResponseBytes == 0)
                return AiPlanningRuntimeConfigurationState.Unconfigured;
            if (!ValidEndpoint(GenerationEndpoint) || !ValidEndpoint(EvaluationEndpoint) ||
                StringComparer.OrdinalIgnoreCase.Equals(GenerationEndpoint, EvaluationEndpoint) ||
                string.IsNullOrWhiteSpace(GenerationRuntimeProfile) || string.IsNullOrWhiteSpace(EvaluationRuntimeProfile) ||
                StringComparer.Ordinal.Equals(GenerationRuntimeProfile, EvaluationRuntimeProfile) ||
                string.IsNullOrWhiteSpace(GenerationOperatorId) || string.IsNullOrWhiteSpace(EvaluationOperatorId) ||
                StringComparer.Ordinal.Equals(GenerationOperatorId, EvaluationOperatorId) ||
                SignatureAlgorithm is not ("RS256" or "ES256") || RequestTimeoutSeconds <= 0 ||
                MaximumRequestBytes <= 0 || MaximumResponseBytes <= 0 ||
                !ValidKeys(PromptTrustedPublicKeysPem) || !ValidKeys(GenerationTrustedPublicKeysPem) ||
                !ValidKeys(EvaluationTrustedPublicKeysPem) ||
                GenerationTrustedPublicKeysPem.Keys.Intersect(EvaluationTrustedPublicKeysPem.Keys, StringComparer.Ordinal).Any())
                return AiPlanningRuntimeConfigurationState.Invalid;
            return AiPlanningRuntimeConfigurationState.Configured;
        }
    }

    public bool IsOperationallyConfigured => ConfigurationState == AiPlanningRuntimeConfigurationState.Configured;

    private static bool ValidEndpoint(string value) => Uri.TryCreate(value, UriKind.Absolute, out var endpoint) &&
        StringComparer.OrdinalIgnoreCase.Equals(endpoint.Scheme, Uri.UriSchemeHttps) &&
        string.IsNullOrEmpty(endpoint.UserInfo) && string.IsNullOrEmpty(endpoint.Query) &&
        string.IsNullOrEmpty(endpoint.Fragment);

    private static bool ValidKeys(Dictionary<string, string> keys) => keys.Count > 0 &&
        keys.All(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value) &&
            pair.Value.Contains("PUBLIC KEY", StringComparison.Ordinal));
}

public sealed record AiPlanningRuntimeReadiness(AiPlanningRuntimeConfigurationState State)
{
    public bool IsOperationallyConfigured => State == AiPlanningRuntimeConfigurationState.Configured;
}
