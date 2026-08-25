using System.Collections.Immutable;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Intelligence;

public sealed record ProactiveIntelligenceSnapshot(
    Guid RequestId,
    string TenantId,
    string VerifiedPolicyId,
    string VerifiedPolicyVersion,
    string VerifiedPolicySha256Digest,
    bool PolicySignatureValid,
    ImmutableArray<EnterpriseObject> Objects,
    ImmutableArray<EnterpriseOperationalSignal> Signals,
    ImmutableArray<string> AuthorizationEvidenceReferences,
    string PolicyVerificationEvidenceReference,
    DateTimeOffset CapturedAt);
