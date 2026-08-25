namespace Platform.Governance.Policies;

public sealed record PolicyBundleVerification(
    string BundleId,
    string Version,
    string Sha256Digest,
    string Environment,
    bool SignatureValid,
    string VerificationEvidenceReference,
    DateTimeOffset VerifiedAt);
