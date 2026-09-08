using System.Collections.Immutable;
using Platform.Evidence.Chain;

namespace Platform.SoftwareFactory.Packages;

public sealed record PackageSupplyChainAttestation(
    string CoordinateSha256Digest,
    string ContentSha256Digest,
    string ProvenanceSha256Digest,
    string SbomSha256Digest,
    string SovereignRegistrySha256Digest,
    SignatureEnvelope Signature,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    ImmutableArray<string> EvidenceReferences)
{
    public PackageSupplyChainAttestation Validate()
    {
        foreach (var digest in new[] { CoordinateSha256Digest, ContentSha256Digest,
                     ProvenanceSha256Digest, SbomSha256Digest, SovereignRegistrySha256Digest })
            if (digest.Length != 64 || digest.Any(character => !Uri.IsHexDigit(character)))
                throw new InvalidOperationException("Package supply-chain attestation requires SHA-256 digests.");
        ArgumentNullException.ThrowIfNull(Signature);
        Signature.Validate();
        if (IssuedAt == default || ExpiresAt <= IssuedAt ||
            Signature.SignedAt < IssuedAt || Signature.SignedAt > ExpiresAt)
            throw new InvalidOperationException("Package supply-chain attestation time is invalid.");
        if (EvidenceReferences.IsDefaultOrEmpty || EvidenceReferences.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Package supply-chain attestation requires evidence.");
        return this;
    }
}
