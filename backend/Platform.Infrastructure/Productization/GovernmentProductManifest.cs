using System.Collections.Immutable;
using Platform.Infrastructure.Sovereignty;

namespace Platform.Infrastructure.Productization;

public sealed record GovernmentProductManifest(
    Guid ProductId,
    string ProductName,
    string ProductVersion,
    string ManifestSha256Digest,
    string SignatureReference,
    bool ExternalLicenseCheckRequired,
    bool ExternalTelemetryRequired,
    bool SupportsOfflineInstallation,
    ImmutableArray<VerifiedDeploymentArtifact> Artifacts,
    ImmutableArray<ComplianceControlMapping> ComplianceControls,
    ImmutableArray<string> ReleaseEvidenceReferences,
    DateTimeOffset ReleasedAt)
{
    public GovernmentProductManifest Validate()
    {
        if (ProductId == Guid.Empty) throw new InvalidOperationException("Government product identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(ProductName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ProductVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureReference);
        if (ManifestSha256Digest.Length != 64 || ManifestSha256Digest.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Product manifest requires a SHA-256 digest.");
        if (ExternalLicenseCheckRequired || ExternalTelemetryRequired || !SupportsOfflineInstallation)
            throw new InvalidOperationException("Government product must operate without external licensing or telemetry and support offline installation.");
        if (Artifacts.IsDefaultOrEmpty || ComplianceControls.IsDefaultOrEmpty || ReleaseEvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Government product artifacts, controls, and release evidence are required.");
        foreach (var artifact in Artifacts) artifact.Validate();
        foreach (var control in ComplianceControls) control.Validate();
        foreach (var value in ReleaseEvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (ReleasedAt == default) throw new InvalidOperationException("Government product release time is required.");
        return this;
    }
}
