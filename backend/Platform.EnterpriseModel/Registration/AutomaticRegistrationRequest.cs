using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Platform.Domain.Security;

namespace Platform.EnterpriseModel.Registration;

public sealed partial record AutomaticRegistrationRequest(
    Guid Id,
    AutomaticRegistrationKey Key,
    string EnterpriseObjectType,
    string OwnerId,
    DataClassification Classification,
    string ArtifactDigest,
    string RegistryReference,
    string DeploymentEvidenceReference,
    string SupplyChainEvidenceReference,
    string ObservabilityEvidenceReference,
    string HumanApprovalReference,
    ImmutableArray<string> PolicyReferences,
    ImmutableArray<string> PermittedActions,
    ImmutableArray<string> EvidenceReferences,
    ImmutableArray<AutomaticRegistrationRelationship> Relationships,
    DateTimeOffset RegisteredAt)
{
    public AutomaticRegistrationRequest Validate()
    {
        if (Id == Guid.Empty) throw new InvalidOperationException("Registration request identity is required.");
        ArgumentNullException.ThrowIfNull(Key);
        Key.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(EnterpriseObjectType);
        ArgumentException.ThrowIfNullOrWhiteSpace(OwnerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ArtifactDigest);
        if (!Sha256Digest().IsMatch(ArtifactDigest))
            throw new InvalidOperationException("Registration requires an algorithm-qualified SHA-256 artifact digest.");
        ArgumentException.ThrowIfNullOrWhiteSpace(RegistryReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(DeploymentEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SupplyChainEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(ObservabilityEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(HumanApprovalReference);
        if (RegisteredAt == default) throw new InvalidOperationException("Registration time is required.");
        if (PolicyReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Registration policy references are required.");
        if (PermittedActions.IsDefaultOrEmpty)
            throw new InvalidOperationException("Registration permitted actions are required.");
        if (EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Registration evidence is required.");
        foreach (var policyReference in PolicyReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(policyReference);
        foreach (var permittedAction in PermittedActions)
            ArgumentException.ThrowIfNullOrWhiteSpace(permittedAction);
        foreach (var evidenceReference in EvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        if (Relationships.IsDefault)
            throw new InvalidOperationException("Registration relationships must be explicitly supplied.");
        foreach (var relationship in Relationships) relationship.Validate();
        return this;
    }

    [GeneratedRegex("^sha256:[0-9a-fA-F]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Digest();
}
