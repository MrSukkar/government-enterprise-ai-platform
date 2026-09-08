using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.InternalService;

public sealed class CryptographicApprovedPackageSupplyChainVerifier(
    IOptions<ApprovedPackagesTrustOptions> configuredOptions) : IApprovedPackageSupplyChainVerifier
{
    public Task<PackageSupplyChainAssuranceDecision> VerifyAsync(
        PackageSupplyChainAssuranceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request); ArgumentNullException.ThrowIfNull(request.Package);
        cancellationToken.ThrowIfCancellationRequested();
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new ApprovedPackagesDependencyUnavailableException("Package-attestation trust is not validly configured.");
        var package = request.Package;
        package.Validate();
        var attestation = package.SupplyChainAttestation
            ?? throw new UnauthorizedAccessException("A signed package supply-chain attestation is required.");
        attestation.Validate();
        if (request.SelectionId == Guid.Empty || string.IsNullOrWhiteSpace(request.TenantId) ||
            string.IsNullOrWhiteSpace(request.Environment) || request.RequestedAt == default ||
            request.EvidenceReferences.IsDefaultOrEmpty || attestation.ExpiresAt <= request.RequestedAt)
            throw new UnauthorizedAccessException("Package supply-chain assurance request or attestation is invalid.");
        if (!package.Coordinate.ContentDigest.StartsWith("sha256:", StringComparison.Ordinal) ||
            package.Coordinate.ContentDigest.Length != 71 || string.IsNullOrWhiteSpace(package.SbomReference) ||
            !package.AvailableInSovereignRegistry)
            throw new UnauthorizedAccessException("Package supply-chain material is incomplete.");

        var coordinateKey = CoordinateKey(package.Coordinate);
        var coordinateDigest = Sha256(coordinateKey);
        var contentDigest = package.Coordinate.ContentDigest[7..].ToLowerInvariant();
        var provenanceDigest = Sha256(string.Join('|', package.Provenance.Source,
            package.Provenance.Publisher, package.Provenance.ProvenanceReference,
            package.Provenance.ObservedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)));
        var sbomDigest = Sha256(package.SbomReference);
        var registryDigest = Sha256(string.Join('|', "sovereign", coordinateKey,
            request.TenantId, request.Environment));
        var bindingsValid = StringComparer.OrdinalIgnoreCase.Equals(attestation.CoordinateSha256Digest, coordinateDigest) &&
            StringComparer.OrdinalIgnoreCase.Equals(attestation.ContentSha256Digest, contentDigest) &&
            StringComparer.OrdinalIgnoreCase.Equals(attestation.ProvenanceSha256Digest, provenanceDigest) &&
            StringComparer.OrdinalIgnoreCase.Equals(attestation.SbomSha256Digest, sbomDigest) &&
            StringComparer.OrdinalIgnoreCase.Equals(attestation.SovereignRegistrySha256Digest, registryDigest);
        var signedPayload = string.Join('|', coordinateDigest, contentDigest, provenanceDigest,
            sbomDigest, registryDigest, attestation.Signature.Algorithm, attestation.Signature.KeyId,
            attestation.Signature.CertificateChainReference,
            attestation.Signature.SignedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            attestation.IssuedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            attestation.ExpiresAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        var signatureValid = bindingsValid && VerifySignature(options, attestation.Signature,
            SHA256.HashData(Encoding.UTF8.GetBytes(signedPayload)));
        if (!signatureValid)
            throw new UnauthorizedAccessException("Package supply-chain attestation signature or binding is invalid.");
        var evidenceDigest = Sha256(string.Join('|', request.SelectionId.ToString("D"), request.TenantId,
            request.Environment, coordinateKey, signedPayload, attestation.Signature.KeyId));
        var evidence = request.EvidenceReferences.Concat(attestation.EvidenceReferences)
            .Append($"evidence://approved-packages/supply-chain/{request.SelectionId:D}/{coordinateDigest}/sha256/{evidenceDigest}")
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        return Task.FromResult(new PackageSupplyChainAssuranceDecision(
            request.SelectionId, package.Coordinate, request.TenantId,
            DigestVerified: true, ProvenanceVerified: true, SbomVerified: true,
            SignatureVerified: true, SovereignRegistryVerified: true,
            PackageTransferred: false, PackageExecuted: false, ExternalEffectOccurred: false,
            evidence, request.RequestedAt));
    }

    private static bool VerifySignature(ApprovedPackagesTrustOptions options,
        Platform.Evidence.Chain.SignatureEnvelope signature, byte[] digest)
    {
        if (!StringComparer.Ordinal.Equals(signature.Algorithm, options.SignatureAlgorithm) ||
            !options.TrustedPublicKeysPem.TryGetValue(signature.KeyId, out var pem)) return false;
        var bytes = Convert.FromBase64String(signature.SignatureBase64);
        if (signature.Algorithm == "RS256")
        {
            using var rsa = RSA.Create(); rsa.ImportFromPem(pem);
            return rsa.VerifyHash(digest, bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        using var ecdsa = ECDsa.Create(); ecdsa.ImportFromPem(pem);
        return ecdsa.VerifyHash(digest, bytes);
    }

    private static string CoordinateKey(PackageCoordinate coordinate) =>
        $"{coordinate.Kind}|{coordinate.Name}|{coordinate.Version}|{coordinate.ContentDigest}";
    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
