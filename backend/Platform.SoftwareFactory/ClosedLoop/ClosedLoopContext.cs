using System.Collections.Immutable;

namespace Platform.SoftwareFactory.ClosedLoop;

public sealed record ClosedLoopContext(
    Guid RequestId,
    string TenantId,
    string EnterpriseObjectReference,
    string ReleaseArtifactSha256Digest,
    string ReleaseProvenanceReference,
    string VerifiedPolicyId,
    string VerifiedPolicyVersion,
    string VerifiedPolicySha256Digest,
    bool PolicySignatureValid,
    ImmutableArray<string> DeliveryEvidenceReferences,
    ImmutableArray<string> RegistrationEvidenceReferences,
    ImmutableArray<string> TelemetryEvidenceReferences,
    string PolicyVerificationEvidenceReference,
    DateTimeOffset CapturedAt);
