using Microsoft.AspNetCore.DataProtection;
using System.Text.Json.Serialization;
using Platform.AgenticWork;
using Platform.Api.Composition;
using Platform.Api.Contracts;
using Platform.Api.Developers;
using Platform.Api.Operations;
using Platform.Api.InternalService;
using Platform.EnterpriseModel;
using Platform.Evidence;
using Platform.Governance;
using Platform.Identity;
using Platform.Infrastructure;
using Platform.Integrations;
using Platform.Knowledge;
using Platform.Modeling;
using Platform.Observability;
using Platform.SoftwareFactory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks();
builder.Services.AddAuthentication();
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
builder.Services.AddPlatformIdentityFoundation(builder.Configuration);
builder.Services.AddPlatformKnowledgeFoundation();
builder.Services.AddPlatformSoftwareFactoryFoundation();
builder.Services.AddPlatformInfrastructureFoundation();
builder.Services.AddPlatformObservabilityFoundation(builder.Configuration);
builder.Services.AddPlatformEnterpriseModelFoundation();
builder.Services.AddPlatformAgenticWorkFoundation();
builder.Services.AddPlatformGovernanceFoundation();
builder.Services.AddPlatformModelingFoundation();
builder.Services.AddPlatformEvidenceFoundation();
builder.Services.AddPlatformModules(
    new IdentityModule(),
    new GovernanceModule(),
    new KnowledgeModule(),
    new EnterpriseModelModule(),
    new SoftwareFactoryModule(),
    new AgenticWorkModule(),
    new ObservabilityModule(),
    new IntegrationsModule(),
    new ModelingModule(),
    new EvidenceModule(),
    new InfrastructureModule());

var runtimeReadiness = PlatformRuntimeReadiness.Inspect(builder.Services);
builder.Services.AddSingleton(runtimeReadiness);

if (builder.Environment.IsDevelopment())
{
    // The development smoke-test host writes only to its captured console stream;
    // governed deployments retain their configured observability providers.
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();

    // Development smoke tests must not depend on or mutate a workstation key ring.
    // Governed environments continue to require their configured sovereign key boundary.
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(
            Path.Combine(AppContext.BaseDirectory, ".development-keys")));

    // Boundary adapters remain fail-closed when resolved. Development startup is
    // allowed so liveness, readiness, the approved contract, and diagnostics can run.
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = false;
    });
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapPlatformOperationalReadiness();
app.MapApprovedOpenApiContract();
app.MapInternalServiceFoundation();
app.MapInternalServiceIntentSubmission();
app.MapInternalServiceIntentRegistration();
app.MapInternalServiceEnterpriseContextDiscovery();

if (app.Environment.IsDevelopment())
{
    app.MapDeveloperPortal();
}

app.Run();

public partial class Program;
