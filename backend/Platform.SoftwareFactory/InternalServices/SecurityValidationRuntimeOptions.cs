using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Platform.Evidence.Chain;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Validation;

namespace Platform.SoftwareFactory.InternalService;

public enum SecurityValidationRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed class SecurityValidationControlProfile
{
    public string ControlId { get; init; } = string.Empty;
    public string[] RequiredText { get; init; } = [];
    public string[] ForbiddenText { get; init; } = [];
    public string[] AllowedFileExtensions { get; init; } = [];
    public string ApprovalEvidenceReference { get; init; } = string.Empty;
    public string SignatureKeyId { get; init; } = string.Empty;
    public string SignatureBase64 { get; init; } = string.Empty;
    public string CertificateChainReference { get; init; } = string.Empty;
    public DateTimeOffset SignedAt { get; init; }

    internal bool IsStructurallyValid => !string.IsNullOrWhiteSpace(ControlId) && ControlId is not ("." or "..") &&
        ControlId.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.') &&
        RequiredText.Concat(ForbiddenText).Any() &&
        RequiredText.Concat(ForbiddenText).All(value => !string.IsNullOrWhiteSpace(value)) &&
        RequiredText.Distinct(StringComparer.Ordinal).Count() == RequiredText.Length &&
        ForbiddenText.Distinct(StringComparer.Ordinal).Count() == ForbiddenText.Length &&
        AllowedFileExtensions.Length > 0 && AllowedFileExtensions.All(value => value.Length > 1 &&
            value[0] == '.' && value[1..].All(char.IsLetterOrDigit)) &&
        AllowedFileExtensions.Distinct(StringComparer.OrdinalIgnoreCase).Count() == AllowedFileExtensions.Length &&
        !string.IsNullOrWhiteSpace(ApprovalEvidenceReference) && !string.IsNullOrWhiteSpace(SignatureKeyId) &&
        !string.IsNullOrWhiteSpace(SignatureBase64) && !string.IsNullOrWhiteSpace(CertificateChainReference) && SignedAt != default;
}

public sealed class SecurityValidationRuntimeOptions
{
    public const string SectionName = "Platform:SoftwareFactory:SecurityValidationRuntime";
    public string SignatureAlgorithm { get; init; } = string.Empty;
    public Dictionary<string, string> TrustedPublicKeysPem { get; init; } = new(StringComparer.Ordinal);
    public SecurityValidationControlProfile[] Controls { get; init; } = [];

    public SecurityValidationRuntimeConfigurationState ConfigurationState
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SignatureAlgorithm) && TrustedPublicKeysPem.Count == 0 && Controls.Length == 0)
                return SecurityValidationRuntimeConfigurationState.Unconfigured;
            if (SignatureAlgorithm is not ("RS256" or "ES256") || TrustedPublicKeysPem.Count == 0 ||
                TrustedPublicKeysPem.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value) ||
                    !pair.Value.Contains("PUBLIC KEY", StringComparison.Ordinal)) || Controls.Length == 0 ||
                Controls.Any(control => !control.IsStructurallyValid) ||
                Controls.Select(control => control.ControlId).Distinct(StringComparer.Ordinal).Count() != Controls.Length)
                return SecurityValidationRuntimeConfigurationState.Invalid;
            return SecurityValidationRuntimeConfigurationState.Configured;
        }
    }

    public bool IsOperationallyConfigured => ConfigurationState == SecurityValidationRuntimeConfigurationState.Configured;
}

public sealed record SecurityValidationRuntimeReadiness(SecurityValidationRuntimeConfigurationState State)
{
    public bool IsOperationallyConfigured => State == SecurityValidationRuntimeConfigurationState.Configured;
}

public sealed class SignedDeterministicSecurityValidationControl(
    SecurityValidationControlProfile profile, SecurityValidationRuntimeOptions options) : ICodeValidationControl
{
    public string ControlId => profile.ControlId;
    public ValidationGate Gate => ValidationGate.Security;

    public Task<ValidationControlReport> ValidateAsync(CodeValidationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request); cancellationToken.ThrowIfCancellationRequested();
        if (!options.IsOperationallyConfigured || !profile.IsStructurallyValid)
            throw new SecurityValidationDependencyUnavailableException("Security control profile is not configured.");
        var signature = new SignatureEnvelope(options.SignatureAlgorithm, profile.SignatureKeyId,
            profile.SignatureBase64, profile.CertificateChainReference, profile.SignedAt);
        var canonical = JsonSerializer.SerializeToUtf8Bytes(new
        {
            profile.ControlId,
            RequiredText = profile.RequiredText.Order(StringComparer.Ordinal).ToArray(),
            ForbiddenText = profile.ForbiddenText.Order(StringComparer.Ordinal).ToArray(),
            AllowedFileExtensions = profile.AllowedFileExtensions.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            profile.ApprovalEvidenceReference,
            signature.Algorithm,
            signature.KeyId,
            signature.CertificateChainReference,
            SignedAt = signature.SignedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
        });
        if (!AiPlanningSignatureVerifier.Verify(options.SignatureAlgorithm, options.TrustedPublicKeysPem,
                signature, SHA256.HashData(canonical)))
            throw new UnauthorizedAccessException("Security control profile signature is invalid.");
        var findings = ImmutableArray.CreateBuilder<ValidationFinding>();
        foreach (var path in request.Candidate.GeneratedFilePaths.Order(StringComparer.Ordinal))
            if (!profile.AllowedFileExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                findings.Add(Finding("allowed-extension", $"Path extension is not authorized by control {ControlId}.", path));
        foreach (var required in profile.RequiredText.Order(StringComparer.Ordinal))
            if (!request.Candidate.Content.Contains(required, StringComparison.Ordinal))
                findings.Add(Finding("required-text", $"Required institutional security rule was not satisfied by control {ControlId}.", "candidate"));
        foreach (var forbidden in profile.ForbiddenText.Order(StringComparer.Ordinal))
            if (request.Candidate.Content.Contains(forbidden, StringComparison.Ordinal))
                findings.Add(Finding("forbidden-text", $"Forbidden institutional security rule was matched by control {ControlId}.", "candidate"));
        var candidateDigest = SovereignHttpCodeGenerationRuntime.CandidateDigest(request.Candidate);
        var evidenceDigest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
            ControlId, candidateDigest, profile.ApprovalEvidenceReference,
            string.Join(',', findings.Select(item => $"{item.RuleId}:{item.Location}:{item.Message}"))))));
        var evidence = $"evidence://security-validation/controls/{ControlId}/sha256/{evidenceDigest}";
        return Task.FromResult(new ValidationControlReport(ControlId, ValidationGate.Security, true,
            findings.Select(item => item with { EvidenceReference = evidence }).ToImmutableArray(), evidence));
    }

    private static ValidationFinding Finding(string rule, string message, string location) =>
        new(rule, ValidationSeverity.Error, message, location, "pending-evidence");
}
