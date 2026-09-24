using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;
using Platform.Integrations.Constitution;
using Platform.Integrations.ApiCatalog;
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

const string validOpenApi = """
{"openapi":"3.1.0","info":{"title":"Synthetic","version":"1.0.0"},"paths":{"/orders":{"get":{"operationId":"getOrders"}}}}
""";
var apiRequest = new ApiCatalogPublicationRequest(
    Guid.Parse("05050505-0505-0505-0505-050505050505"), "subject://synthetic-publisher", contract,
    validOpenApi, 0, DateTimeOffset.Parse("2026-09-24T00:02:00Z"));
var apiPolicy = new VerifyingApiCatalogPolicyAuthorizer { Permit = true };
var apiRepository = new VerifyingApiCatalogRepository();
var apiRelease = new VerifyingApiCatalogResultAuthorizer { Permit = true };
var apiCatalog = new GovernedApiCatalog(new StrictOpenApi31ContractValidator(), apiPolicy, apiRepository, apiRelease);
var publication = await apiCatalog.PublishAsync(apiRequest, CancellationToken.None);
if (publication.Lifecycle != ApiLifecycleState.Published || apiRepository.Mutations != 1 || apiRelease.Calls != 1)
    throw new InvalidOperationException("Permitted API publication was not atomically committed and released.");

await ExpectDeniedAsync(() => apiCatalog.PublishAsync(apiRequest with
{
    RequestId = Guid.NewGuid(), OpenApiDocument = validOpenApi.Replace("3.1.0", "3.0.3", StringComparison.Ordinal)
}, CancellationToken.None), "OpenAPI 3.0 document");
await ExpectDeniedAsync(() => apiCatalog.PublishAsync(apiRequest with
{
    RequestId = Guid.NewGuid(),
    OpenApiDocument = """
    {"openapi":"3.1.0","info":{"title":"Synthetic","version":"1.0.0"},"paths":{"/a":{"get":{"operationId":"duplicate"}},"/b":{"post":{"operationId":"duplicate"}}}}
    """
}, CancellationToken.None), "duplicate operationId");
if (apiRepository.Mutations != 1) throw new InvalidOperationException("Invalid OpenAPI reached persistence.");

apiPolicy.Permit = false;
await ExpectDeniedAsync(() => apiCatalog.PublishAsync(apiRequest with { RequestId = Guid.NewGuid() }, CancellationToken.None), "API catalog OPA denial");
if (apiRepository.Mutations != 1) throw new InvalidOperationException("Denied API publication reached persistence.");

apiPolicy.Permit = true;
apiRelease.Permit = false;
await ExpectDeniedAsync(() => apiCatalog.PublishAsync(apiRequest with { RequestId = Guid.NewGuid() }, CancellationToken.None), "result release denial");
if (apiRepository.Mutations != 2 || apiRelease.Calls != 2)
    throw new InvalidOperationException("Result release authorization did not follow the atomic commit exactly once.");

_ = new ApiLifecycleTransitionRequest(ApiLifecycleState.Proposed, ApiLifecycleState.Published).Validate();
await ExpectDeniedAsync(() => Task.Run(() => new ApiLifecycleTransitionRequest(ApiLifecycleState.Published, ApiLifecycleState.Proposed).Validate()), "API lifecycle rollback");

Console.WriteLine("V3-03 API CATALOG VERIFIED: strict OpenAPI 3.1, OPA-before-mutation, atomic publication, result-release authorization, and forward-only lifecycle enforced.");

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

sealed class VerifyingApiCatalogPolicyAuthorizer : IApiCatalogPolicyAuthorizer
{
    public bool Permit { get; set; }

    public Task<ApiCatalogPolicyDecision> AuthorizePublicationAsync(ApiCatalogPublicationRequest request, OpenApi31ValidationReport report, CancellationToken cancellationToken) =>
        Task.FromResult(new ApiCatalogPolicyDecision(Guid.NewGuid(), "integration.api.catalog.publish",
            request.ComputeFingerprint(report.DocumentSha256Digest), report.DocumentSha256Digest,
            request.Contract.TenantId, request.Contract.Purpose, request.Contract.Environment, Permit,
            "opa://bundle/v3-03", ["evidence://opa/v3-03"]));
}

sealed class VerifyingApiCatalogRepository : IApiCatalogRepository
{
    public int Mutations { get; private set; }

    public Task<ApiCatalogCommit> PublishAtomicallyAsync(ApiCatalogPublicationRequest request, OpenApi31ValidationReport report, ApiCatalogPolicyDecision decision, CancellationToken cancellationToken)
    {
        Mutations++;
        return Task.FromResult(new ApiCatalogCommit(Guid.NewGuid(), request.RequestId, request.Contract.ContractId,
            request.Contract.Version, report.DocumentSha256Digest, ApiLifecycleState.Published, request.ExpectedVersion + 1,
            report.OperationIds, ["evidence://catalog/v3-03"], DateTimeOffset.Parse("2026-09-24T00:02:01Z")));
    }
}

sealed class VerifyingApiCatalogResultAuthorizer : IApiCatalogResultAuthorizer
{
    public bool Permit { get; set; }
    public int Calls { get; private set; }

    public Task<ApiCatalogReleaseDecision> AuthorizeReleaseAsync(ApiCatalogCommit commit, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(new ApiCatalogReleaseDecision(Guid.NewGuid(), commit.CatalogEntryId,
            commit.DocumentSha256Digest, Permit, ["evidence://release/v3-03"]));
    }
}
