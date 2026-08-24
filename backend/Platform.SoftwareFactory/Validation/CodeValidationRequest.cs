using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.Validation;

public sealed record CodeValidationRequest(
    SoftwareDeliveryRun Run,
    AiCandidateArtifact Candidate,
    ValidationGate Gate);
