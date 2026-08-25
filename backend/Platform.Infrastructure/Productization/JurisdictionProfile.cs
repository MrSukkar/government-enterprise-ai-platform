using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.Infrastructure.Sovereignty;

namespace Platform.Infrastructure.Productization;

public sealed record JurisdictionProfile(
    string JurisdictionCode,
    string DataResidencyReference,
    ImmutableArray<string> SupportedLanguages,
    DataClassification MaximumClassification,
    ImmutableHashSet<DeploymentTopology> AllowedTopologies,
    string IdentityAuthorityReference,
    string PolicyAuthorityReference,
    string TrustBundleReference,
    ImmutableHashSet<string> RequiredComplianceControls)
{
    public JurisdictionProfile Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(JurisdictionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(DataResidencyReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(IdentityAuthorityReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(PolicyAuthorityReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(TrustBundleReference);
        if (SupportedLanguages.IsDefaultOrEmpty || AllowedTopologies.IsEmpty || RequiredComplianceControls.IsEmpty)
            throw new InvalidOperationException("Jurisdiction languages, topologies, and compliance controls are required.");
        foreach (var value in SupportedLanguages) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var value in RequiredComplianceControls) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return this;
    }
}
