using Platform.AgenticWork;
using Platform.Api.Composition;
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
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
