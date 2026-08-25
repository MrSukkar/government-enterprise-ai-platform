namespace Platform.Infrastructure.Sovereignty;

public sealed record SovereignDeploymentReceipt(
    Guid RequestId,
    Guid ProfileId,
    string ArtifactDigest,
    string RuntimeIdentity,
    string EvidenceReference,
    DateTimeOffset CompletedAt);
