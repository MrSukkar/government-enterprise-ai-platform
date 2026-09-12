using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Governance.Policies;

namespace Platform.SoftwareFactory.InternalService;

public enum AutomaticRegistrationRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed class AutomaticRegistrationRuntimeOptions
{
    public const string SectionName = "Platform:SoftwareFactory:AutomaticRegistrationRuntime";
    public Guid ManifestId { get; init; }
    public string ManifestVersion { get; init; } = string.Empty;
    public string ManifestSha256Digest { get; init; } = string.Empty;
    public string RuntimeIdentity { get; init; } = string.Empty;
    public string ServiceIdentity { get; init; } = string.Empty;
    public string ServiceVersion { get; init; } = string.Empty;
    public string ArtifactDigest { get; init; } = string.Empty;

    public AutomaticRegistrationRuntimeConfigurationState ConfigurationState
    {
        get
        {
            var values = new[] { ManifestVersion, ManifestSha256Digest, RuntimeIdentity,
                ServiceIdentity, ServiceVersion, ArtifactDigest };
            if (ManifestId == Guid.Empty && values.All(string.IsNullOrWhiteSpace))
                return AutomaticRegistrationRuntimeConfigurationState.Unconfigured;
            if (ManifestId == Guid.Empty || new[] { ManifestVersion, RuntimeIdentity, ServiceIdentity,
                    ServiceVersion }.Any(string.IsNullOrWhiteSpace) ||
                !IsDigest(ManifestSha256Digest) || !IsDigest(ArtifactDigest))
                return AutomaticRegistrationRuntimeConfigurationState.Invalid;
            return AutomaticRegistrationRuntimeConfigurationState.Configured;
        }
    }

    public bool IsOperationallyConfigured => ConfigurationState == AutomaticRegistrationRuntimeConfigurationState.Configured;
    private static bool IsDigest(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
}

public sealed record AutomaticRegistrationRuntimeReadiness(AutomaticRegistrationRuntimeConfigurationState State);
public sealed class AutomaticRegistrationDependencyUnavailableException(string message) : Exception(message);

public sealed class SovereignAutomaticRegistrationPolicyGate(
    IPolicyBundleVerifier bundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : IAutomaticRegistrationPolicyGate
{
    public async Task<AutomaticRegistrationPolicyDecision> EvaluateAsync(
        AutomaticRegistrationPolicyInput input, CancellationToken cancellationToken)
    {
        var verified = await bundleVerifier.VerifyAsync(new(input.PolicyBundle.BundleId,
            input.PolicyBundle.Version, input.PolicyBundle.Sha256Digest,
            input.PolicyBundle.SignatureReference, input.PolicyBundle.Environment,
            input.PolicyBundle.ActivatedAt), cancellationToken);
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["registrationId"] = input.RegistrationId.ToString("D"),
            ["activationId"] = input.ActivationId.ToString("D"),
            ["deliveryRunId"] = input.DeliveryRunId.ToString("D"),
            ["manifestId"] = input.ManifestId.ToString("D"),
            ["manifestVersion"] = input.ManifestVersion,
            ["manifestSha256Digest"] = input.ManifestSha256Digest,
            ["runtimeIdentity"] = input.RuntimeIdentity,
            ["serviceIdentity"] = input.ServiceIdentity,
            ["serviceVersion"] = input.ServiceVersion,
            ["artifactDigest"] = input.ArtifactDigest,
            ["openTelemetryEvidenceReference"] = input.OpenTelemetryEvidenceReference
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence = input.EvidenceReferences.Append(verified.VerificationEvidenceReference)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var decision = await policyClient.EvaluateAsync(new(input.DecisionRequestId,
            "internal-service.automatic-registration.execute", input.RegistrationId.ToString("D"),
            input.TenantId, input.SubjectId, input.Purpose, input.MaximumClassification.ToString(),
            input.Environment, verified, attributes, evidence,
            verified.VerifiedAt > input.EvaluatedAt ? verified.VerifiedAt : input.EvaluatedAt), cancellationToken);
        if (decision.Outcome == OpaDecisionOutcome.Deny)
        {
            if (decision.Scope is not null)
                throw new UnauthorizedAccessException("Denied Automatic Registration decision returned scope.");
            return Create(input, verified, decision, GovernedIntentPolicyOutcome.Deny,
                input.MaximumClassification, false, false);
        }

        var envelope = decision.Scope ?? throw new UnauthorizedAccessException("Automatic Registration scope missing.");
        var scope = envelope.AutomaticRegistration ?? throw new UnauthorizedAccessException("Automatic Registration action scope missing.");
        if (envelope.OpenTelemetry is not null || envelope.Deployment is not null || envelope.Artifact is not null ||
            envelope.CiCd is not null || envelope.Git is not null || envelope.HumanReview is not null ||
            envelope.Tests is not null || envelope.Sandbox is not null || envelope.SecurityValidation is not null ||
            envelope.StaticValidation is not null || envelope.CodeGeneration is not null || envelope.AiPlanning is not null ||
            envelope.ApprovedPackages is not null || envelope.ExistingArchitecture is not null || envelope.ExistingSystems is not null)
            throw new UnauthorizedAccessException("Automatic Registration decision mixed scopes.");
        if (!Enum.TryParse<DataClassification>(scope.MaximumClassification, true, out var classification) ||
            scope.ActivationId != input.ActivationId || scope.DeliveryRunId != input.DeliveryRunId ||
            scope.ManifestId != input.ManifestId || scope.ManifestVersion != input.ManifestVersion ||
            !StringComparer.OrdinalIgnoreCase.Equals(scope.ManifestSha256Digest, input.ManifestSha256Digest) ||
            scope.RuntimeIdentity != input.RuntimeIdentity || scope.ServiceIdentity != input.ServiceIdentity ||
            scope.ServiceVersion != input.ServiceVersion ||
            !StringComparer.OrdinalIgnoreCase.Equals(scope.ArtifactDigest, input.ArtifactDigest) ||
            scope.OpenTelemetryEvidenceReference != input.OpenTelemetryEvidenceReference ||
            !scope.RegistrationAllowed || scope.WorkflowAdvancementAllowed || scope.RequiredRoles.IsDefaultOrEmpty ||
            scope.OutputKind != "automatic-registration-result")
            throw new UnauthorizedAccessException("Automatic Registration scope mismatch.");
        return Create(input, verified, decision, GovernedIntentPolicyOutcome.Permit,
            classification, scope.RegistrationAllowed, scope.WorkflowAdvancementAllowed);
    }

    private static AutomaticRegistrationPolicyDecision Create(
        AutomaticRegistrationPolicyInput input, PolicyBundleVerification verified,
        SovereignPolicyEvaluationDecision decision, GovernedIntentPolicyOutcome outcome,
        DataClassification classification, bool registrationAllowed, bool workflowAdvancementAllowed) => new(
            input.DecisionRequestId, input.RegistrationId, input.ActivationId, input.DeliveryRunId,
            input.ManifestId, input.ManifestVersion, input.ManifestSha256Digest, input.TenantId,
            input.Environment, input.RuntimeIdentity, input.ServiceIdentity, input.ServiceVersion,
            input.ArtifactDigest, registrationAllowed, workflowAdvancementAllowed,
            verified.BundleId, verified.Version, verified.Sha256Digest, true,
            verified.VerificationEvidenceReference, outcome, classification,
            decision.Reasons, decision.EvidenceReferences, decision.DecidedAt);
}

public sealed class DeterministicAutomaticRegistrationResultAuthorizer : IAutomaticRegistrationResultAuthorizer
{
    public Task<AutomaticRegistrationResultAuthorizationDecision> AuthorizeAsync(
        AutomaticRegistrationResultAuthorizationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        request.Commit.EnterpriseObject.Validate();
        if (!Enum.IsDefined(request.Commit.Disposition) ||
            request.Commit.RequestId != request.RegistrationId ||
            request.Commit.EnterpriseObject.TenantId != request.TenantId ||
            request.Commit.EnterpriseObject.Source != "automatic-registration" ||
            request.Commit.EnterpriseObject.State != "registered" ||
            request.Commit.EnterpriseObject.Confidence != 1m ||
            string.IsNullOrWhiteSpace(request.Commit.RequestFingerprint) ||
            string.IsNullOrWhiteSpace(request.Commit.EvidenceReference))
            throw new UnauthorizedAccessException("Unsafe Automatic Registration result.");
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{request.AuthorizationRequestId:D}|{request.Commit.RequestFingerprint}|{request.SubjectId}")));
        return Task.FromResult(new AutomaticRegistrationResultAuthorizationDecision(
            request.AuthorizationRequestId, request.RegistrationId, request.TenantId,
            request.Commit.RequestFingerprint, true, "automatic-registration-result-authorized",
            request.EvidenceReferences.Append($"evidence://automatic-registration/result-authorization/{request.AuthorizationRequestId:D}/sha256/{digest}").ToImmutableArray(),
            request.RequestedAt));
    }
}
