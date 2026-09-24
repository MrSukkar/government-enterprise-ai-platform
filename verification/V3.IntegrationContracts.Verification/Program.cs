using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;
using Platform.Integrations.Constitution;

var required = new IntegrationSemanticRule(IntegrationSemanticDisposition.Required, "policy://delivery/required");
var notApplicable = new IntegrationSemanticRule(IntegrationSemanticDisposition.NotApplicable, null);
var contract = new IntegrationConstitutionalContract
{
    ContractId = "integration://api/internal-services/v1",
    Version = "1.0.0",
    Kind = IntegrationAssetKind.Api,
    EnterpriseObjectId = new EnterpriseObjectId(Guid.Parse("01010101-0101-0101-0101-010101010101")),
    OwnerId = "owner://integration-platform",
    TenantId = "tenant-synthetic",
    Purpose = "integration-contract-verification",
    Environment = "verification",
    Lifecycle = LifecycleState.Proposed,
    Classification = DataClassification.Internal,
    InputSchemaReferences = ["schema://request/v1"],
    OutputSchemaReferences = ["schema://response/v1"],
    CompatibilityPolicyReferences = ["policy://schema/compatibility/v1"],
    AuthenticationSchemes = ["oidc://workload"],
    RequiredPermissions = ["integration.api.invoke"],
    PolicyReferences = ["opa://integration/api/v1"],
    ResidencyPolicyReference = "policy://data/residency/v1",
    RetentionPolicyReference = "policy://data/retention/v1",
    EncryptionPolicyReference = "policy://data/encryption/v1",
    RedactionPolicyReference = "policy://telemetry/redaction/v1",
    DeliverySemantics = new IntegrationDeliverySemantics(required, notApplicable, required, required, notApplicable, notApplicable),
    OpenTelemetryResourceIdentity = "service.name=integration-contract-verification",
    EvidenceRequirements = ["evidence://integration/contract-validation"],
    EnterpriseModelRegistrationReference = "enterprise-model://integration/api/internal-services/v1",
    AnonymousAccessAllowed = false,
    AiWorkflowAuthorityAllowed = false,
    ProductionEffectAuthorized = false
}.Validate();

if (contract.Kind != IntegrationAssetKind.Api)
{
    throw new InvalidOperationException("Permitted contract validation failed.");
}

foreach (var kind in Enum.GetValues<IntegrationAssetKind>())
{
    if (string.IsNullOrWhiteSpace(IntegrationEnterpriseModelTypes.GetObjectType(kind)))
    {
        throw new InvalidOperationException($"Enterprise Model mapping missing for {kind}.");
    }
}

ExpectDenied(contract with { AnonymousAccessAllowed = true }, "anonymous access");
ExpectDenied(contract with { AiWorkflowAuthorityAllowed = true }, "AI workflow authority");
ExpectDenied(contract with { ProductionEffectAuthorized = true }, "Production effect");
ExpectDenied(contract with { PolicyReferences = ImmutableArray<string>.Empty }, "missing OPA policy");
ExpectDenied(contract with { EvidenceRequirements = ["evidence://duplicate", "evidence://duplicate"] }, "duplicate evidence");

Console.WriteLine("V3-01 CONTRACTS VERIFIED: permitted contract accepted; denied and incomplete contracts fail closed; all seven Enterprise Model types mapped.");

static void ExpectDenied(IntegrationConstitutionalContract denied, string caseName)
{
    try
    {
        denied.Validate();
    }
    catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
    {
        return;
    }

    throw new InvalidOperationException($"Denied case did not fail closed: {caseName}.");
}
