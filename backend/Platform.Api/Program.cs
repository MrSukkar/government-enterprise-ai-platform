using Platform.AgenticWork;
using Platform.Api.Composition;
using Platform.Api.Contracts;
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

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapApprovedOpenApiContract();

app.Run();

public partial class Program;
