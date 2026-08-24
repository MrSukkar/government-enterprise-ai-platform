using System.Collections.Immutable;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.Sandbox;

public sealed record SandboxExecutionRequest(
    SoftwareDeliveryRun Run,
    AiCandidateArtifact Candidate,
    PackageCoordinate SandboxImage,
    PackageUseDecision SandboxImageDecision,
    SandboxIsolationPolicy IsolationPolicy,
    ImmutableDictionary<string, string> NonSecretEnvironmentReferences);
