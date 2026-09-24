using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;

namespace Platform.Integrations.Constitution;

public sealed record IntegrationConstitutionalContract
{
    public required string ContractId { get; init; }
    public required string Version { get; init; }
    public required IntegrationAssetKind Kind { get; init; }
    public required EnterpriseObjectId EnterpriseObjectId { get; init; }
    public required string OwnerId { get; init; }
    public required string TenantId { get; init; }
    public required string Purpose { get; init; }
    public required string Environment { get; init; }
    public required LifecycleState Lifecycle { get; init; }
    public required DataClassification Classification { get; init; }
    public required ImmutableArray<string> InputSchemaReferences { get; init; }
    public required ImmutableArray<string> OutputSchemaReferences { get; init; }
    public required ImmutableArray<string> CompatibilityPolicyReferences { get; init; }
    public required ImmutableArray<string> AuthenticationSchemes { get; init; }
    public required ImmutableArray<string> RequiredPermissions { get; init; }
    public required ImmutableArray<string> PolicyReferences { get; init; }
    public required string ResidencyPolicyReference { get; init; }
    public required string RetentionPolicyReference { get; init; }
    public required string EncryptionPolicyReference { get; init; }
    public required string RedactionPolicyReference { get; init; }
    public required IntegrationDeliverySemantics DeliverySemantics { get; init; }
    public required string OpenTelemetryResourceIdentity { get; init; }
    public required ImmutableArray<string> EvidenceRequirements { get; init; }
    public required string EnterpriseModelRegistrationReference { get; init; }
    public required bool AnonymousAccessAllowed { get; init; }
    public required bool AiWorkflowAuthorityAllowed { get; init; }
    public required bool ProductionEffectAuthorized { get; init; }

    public IntegrationConstitutionalContract Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ContractId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(OwnerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(ResidencyPolicyReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(RetentionPolicyReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(EncryptionPolicyReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(RedactionPolicyReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(OpenTelemetryResourceIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(EnterpriseModelRegistrationReference);

        if (!System.Version.TryParse(Version, out var parsedVersion) || parsedVersion.Major < 1)
        {
            throw new ArgumentException("Contract version must be an explicit version of at least 1.0.", nameof(Version));
        }

        if (!Enum.IsDefined(Kind))
        {
            throw new ArgumentOutOfRangeException(nameof(Kind), "Unknown integration asset kind fails closed.");
        }

        if (EnterpriseObjectId.Value == Guid.Empty)
        {
            throw new ArgumentException("Enterprise Object identity is required.", nameof(EnterpriseObjectId));
        }

        if (!Enum.IsDefined(Lifecycle) || !Enum.IsDefined(Classification))
        {
            throw new InvalidOperationException("Unknown lifecycle or classification fails closed.");
        }

        ValidateReferences(InputSchemaReferences, nameof(InputSchemaReferences));
        ValidateReferences(OutputSchemaReferences, nameof(OutputSchemaReferences));
        ValidateReferences(CompatibilityPolicyReferences, nameof(CompatibilityPolicyReferences));
        ValidateReferences(AuthenticationSchemes, nameof(AuthenticationSchemes));
        ValidateReferences(RequiredPermissions, nameof(RequiredPermissions));
        ValidateReferences(PolicyReferences, nameof(PolicyReferences));
        ValidateReferences(EvidenceRequirements, nameof(EvidenceRequirements));

        ArgumentNullException.ThrowIfNull(DeliverySemantics);
        DeliverySemantics.Validate();
        _ = IntegrationEnterpriseModelTypes.GetObjectType(Kind);

        if (AnonymousAccessAllowed)
        {
            throw new InvalidOperationException("Anonymous institutional access is prohibited.");
        }

        if (AiWorkflowAuthorityAllowed)
        {
            throw new InvalidOperationException("AI and LLMs cannot hold policy or workflow authority.");
        }

        if (ProductionEffectAuthorized)
        {
            throw new InvalidOperationException("A constitutional contract cannot authorize a Production effect.");
        }

        return this;
    }

    private static void ValidateReferences(ImmutableArray<string> references, string name)
    {
        if (references.IsDefaultOrEmpty || references.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException($"{name} must contain explicit non-empty references.", name);
        }

        if (references.Distinct(StringComparer.Ordinal).Count() != references.Length)
        {
            throw new ArgumentException($"{name} cannot contain duplicate references.", name);
        }
    }
}
