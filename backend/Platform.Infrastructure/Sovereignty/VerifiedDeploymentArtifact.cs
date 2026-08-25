using System.Text.RegularExpressions;

namespace Platform.Infrastructure.Sovereignty;

public sealed partial record VerifiedDeploymentArtifact(
    string Name,
    string ContentDigest,
    string RegistryReference,
    string SbomReference,
    string BuildAttestationReference,
    string SignatureReference,
    string SupplyChainVerificationEvidenceReference)
{
    public VerifiedDeploymentArtifact Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(ContentDigest);
        ArgumentException.ThrowIfNullOrWhiteSpace(RegistryReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SbomReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(BuildAttestationReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SupplyChainVerificationEvidenceReference);
        if (!Sha256Digest().IsMatch(ContentDigest))
            throw new InvalidOperationException("Deployment artifacts require an algorithm-qualified SHA-256 digest.");
        return this;
    }

    [GeneratedRegex("^sha256:[0-9a-fA-F]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Digest();
}
