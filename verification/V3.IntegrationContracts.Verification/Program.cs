using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;
using Platform.Integrations.Constitution;
using Platform.Integrations.Registry;

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

var repository = new VerifyingRepository();
var authorizer = new VerifyingAuthorizer { Permit = true };
var registry = new GovernedConsumerChannelRegistry(authorizer, repository);
var registrationRequest = new ConsumerChannelRegistrationRequest(
    Guid.Parse("02020202-0202-0202-0202-020202020202"),
    "subject://synthetic-registrar",
    contract with { Kind = IntegrationAssetKind.Consumer, Lifecycle = LifecycleState.Proposed },
    0,
    DateTimeOffset.Parse("2026-09-24T00:00:00Z"));
var registration = await registry.RegisterAsync(registrationRequest, CancellationToken.None);
if (registration.Disposition != ConsumerChannelRegistrationDisposition.Created || repository.RegistrationMutations != 1)
    throw new InvalidOperationException("Permitted registration was not atomically committed.");

authorizer.Permit = false;
await ExpectDeniedAsync(() => registry.RegisterAsync(registrationRequest with { RequestId = Guid.NewGuid() }, CancellationToken.None), "OPA denial");
if (repository.RegistrationMutations != 1) throw new InvalidOperationException("Denied registration reached persistence.");

authorizer.Permit = true;
authorizer.MismatchScope = true;
await ExpectDeniedAsync(() => registry.RegisterAsync(registrationRequest with { RequestId = Guid.NewGuid() }, CancellationToken.None), "scope mismatch");
if (repository.RegistrationMutations != 1) throw new InvalidOperationException("Mismatched policy scope reached persistence.");

authorizer.MismatchScope = false;
var lifecycleRequest = new ConsumerChannelLifecycleRequest(
    Guid.Parse("03030303-0303-0303-0303-030303030303"), registration.RegistrationId,
    registrationRequest.Contract.ContractId, IntegrationAssetKind.Consumer, registrationRequest.SubjectId,
    registrationRequest.Contract.TenantId, registrationRequest.Contract.Purpose, registrationRequest.Contract.Environment,
    LifecycleState.Proposed, LifecycleState.Active, registration.Version, DateTimeOffset.Parse("2026-09-24T00:01:00Z"));
var transition = await registry.TransitionAsync(lifecycleRequest, CancellationToken.None);
if (transition.To != LifecycleState.Active || repository.LifecycleMutations != 1)
    throw new InvalidOperationException("Permitted lifecycle transition was not atomically committed.");

await ExpectDeniedAsync(() => registry.TransitionAsync(lifecycleRequest with
{
    TransitionId = Guid.NewGuid(), From = LifecycleState.Active, To = LifecycleState.Proposed
}, CancellationToken.None), "lifecycle rollback");
if (repository.LifecycleMutations != 1) throw new InvalidOperationException("Invalid lifecycle transition reached policy or persistence.");

Console.WriteLine("V3-02 REGISTRY VERIFIED: permitted registration and lifecycle transition committed atomically; denial, scope mismatch, and rollback fail closed without mutation.");

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

static async Task ExpectDeniedAsync(Func<Task> action, string caseName)
{
    try
    {
        await action();
    }
    catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or UnauthorizedAccessException)
    {
        return;
    }
    throw new InvalidOperationException($"Denied async case did not fail closed: {caseName}.");
}

sealed class VerifyingAuthorizer : IConsumerChannelPolicyAuthorizer
{
    public bool Permit { get; set; }
    public bool MismatchScope { get; set; }

    public Task<ConsumerChannelPolicyDecision> AuthorizeRegistrationAsync(ConsumerChannelRegistrationRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(Create("integration.consumer-channel.register", request.ComputeFingerprint(), request.Contract.TenantId,
            request.Contract.Purpose, request.Contract.Environment));

    public Task<ConsumerChannelPolicyDecision> AuthorizeLifecycleAsync(ConsumerChannelLifecycleRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(Create("integration.consumer-channel.lifecycle", request.ComputeFingerprint(), request.TenantId,
            request.Purpose, request.Environment));

    private ConsumerChannelPolicyDecision Create(string action, string fingerprint, string tenant, string purpose, string environment) =>
        new(Guid.NewGuid(), action, MismatchScope ? $"mismatch-{fingerprint}" : fingerprint, tenant, purpose, environment,
            Permit, "opa://bundle/v3-02", ["evidence://opa/v3-02"]);
}

sealed class VerifyingRepository : IConsumerChannelRegistryRepository
{
    public int RegistrationMutations { get; private set; }
    public int LifecycleMutations { get; private set; }

    public Task<ConsumerChannelRegistrationCommit> RegisterAtomicallyAsync(ConsumerChannelRegistrationRequest request, ConsumerChannelPolicyDecision decision, CancellationToken cancellationToken)
    {
        RegistrationMutations++;
        return Task.FromResult(new ConsumerChannelRegistrationCommit(
            Guid.Parse("04040404-0404-0404-0404-040404040404"), request.RequestId, request.Contract.ContractId,
            request.ComputeFingerprint(), 1, ConsumerChannelRegistrationDisposition.Created,
            ["evidence://registry/v3-02"], DateTimeOffset.Parse("2026-09-24T00:00:01Z")));
    }

    public Task<ConsumerChannelLifecycleCommit> TransitionAtomicallyAsync(ConsumerChannelLifecycleRequest request, ConsumerChannelPolicyDecision decision, CancellationToken cancellationToken)
    {
        LifecycleMutations++;
        return Task.FromResult(new ConsumerChannelLifecycleCommit(
            request.TransitionId, request.RegistrationId, request.ComputeFingerprint(), request.From, request.To,
            request.ExpectedVersion + 1, ["evidence://lifecycle/v3-02"], DateTimeOffset.Parse("2026-09-24T00:01:01Z")));
    }
}
