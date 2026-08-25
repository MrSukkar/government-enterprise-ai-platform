namespace Platform.SoftwareFactory.DeveloperExperience;

public sealed record DeveloperTemplateVerification(
    string TemplateId,
    string Version,
    string Sha256Digest,
    bool SignatureValid,
    string VerificationEvidenceReference,
    DateTimeOffset VerifiedAt);
