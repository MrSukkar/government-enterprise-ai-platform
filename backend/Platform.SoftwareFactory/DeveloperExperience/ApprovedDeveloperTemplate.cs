using System.Collections.Immutable;

namespace Platform.SoftwareFactory.DeveloperExperience;

public sealed record ApprovedDeveloperTemplate(
    string TemplateId,
    string Version,
    string Sha256Digest,
    string SignatureReference,
    string ArchitectureReference,
    string RequiredDotNetSdkVersion,
    ImmutableArray<string> ApprovedPackageReferences,
    ImmutableArray<string> EvidenceReferences)
{
    public ApprovedDeveloperTemplate Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(TemplateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(ArchitectureReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(RequiredDotNetSdkVersion);
        if (Sha256Digest.Length != 64 || Sha256Digest.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Developer template requires a SHA-256 digest.");
        if (ApprovedPackageReferences.IsDefault || EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Template packages and evidence must be explicit.");
        foreach (var value in ApprovedPackageReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var value in EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return this;
    }
}
