namespace Platform.Infrastructure.Productization;

public sealed record ProductManifestVerification(
    Guid ProductId,
    string ProductVersion,
    string ManifestSha256Digest,
    string TrustBundleReference,
    bool SignatureValid,
    string VerificationEvidenceReference,
    DateTimeOffset VerifiedAt);
