namespace Platform.Api.Operations;

internal static class PlatformOperationalEndpoints
{
    internal static IEndpointConventionBuilder MapPlatformOperationalReadiness(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGet("/health/ready", (PlatformRuntimeReadiness readiness,
            Platform.Identity.IdentityControlPlaneReadiness identity,
            Platform.Governance.Policies.PolicyControlPlaneReadiness policy,
            Platform.SoftwareFactory.Persistence.PostgreSqlIntentRegistrationReadiness persistence,
            Platform.Knowledge.Retrieval.Neo4jEnterpriseGraphReadiness graph,
            Platform.SoftwareFactory.InternalService.EnterpriseContextRuntimeReadiness enterpriseContext,
            Platform.SoftwareFactory.InternalService.ExistingSystemsRuntimeReadiness existingSystems,
            Platform.SoftwareFactory.InternalService.ExistingArchitectureRuntimeReadiness existingArchitecture,
            Platform.SoftwareFactory.InternalService.ApprovedPackagesRuntimeReadiness approvedPackages,
            Platform.SoftwareFactory.InternalService.AiPlanningRuntimeReadiness aiPlanning,
            Platform.SoftwareFactory.InternalService.CodeGenerationRuntimeReadiness codeGeneration,
            Platform.SoftwareFactory.InternalService.StaticValidationRuntimeReadiness staticValidation,
            Platform.SoftwareFactory.InternalService.SecurityValidationRuntimeReadiness securityValidation,
            Platform.SoftwareFactory.InternalService.SandboxRuntimeReadiness sandbox,
            Platform.SoftwareFactory.InternalService.TestsRuntimeReadiness tests,
            Platform.SoftwareFactory.InternalService.HumanReviewRuntimeReadiness humanReview,
            Platform.SoftwareFactory.InternalService.GitRuntimeReadiness git) =>
        {
            var payload = new
            {
                status = readiness.IsReady ? "ready" : "not-ready",
                failClosed = true,
                controlPlanes = new
                {
                    identity = identity.State.ToString().ToLowerInvariant(),
                    policy = policy.State.ToString().ToLowerInvariant(),
                    postgresqlIntentRegistration = persistence.State.ToString().ToLowerInvariant(),
                    neo4jEnterpriseGraph = graph.State.ToString().ToLowerInvariant(),
                    enterpriseContext = enterpriseContext.State.ToString().ToLowerInvariant(),
                    existingSystems = existingSystems.State.ToString().ToLowerInvariant(),
                    existingArchitecture = existingArchitecture.State.ToString().ToLowerInvariant(),
                    approvedPackages = approvedPackages.State.ToString().ToLowerInvariant(),
                    aiPlanning = aiPlanning.State.ToString().ToLowerInvariant(),
                    codeGeneration = codeGeneration.State.ToString().ToLowerInvariant(),
                    staticValidation = staticValidation.State.ToString().ToLowerInvariant(),
                    securityValidation = securityValidation.State.ToString().ToLowerInvariant(),
                    sandbox = sandbox.State.ToString().ToLowerInvariant(),
                    tests = tests.State.ToString().ToLowerInvariant(),
                    humanReview = humanReview.State.ToString().ToLowerInvariant(),
                    git = git.State.ToString().ToLowerInvariant()
                },
                missingDependencyCount = readiness.MissingDependencies.Count,
                dependencies = readiness.Dependencies.Select(dependency => new
                {
                    capability = dependency.Capability,
                    contract = dependency.Contract,
                    status = dependency.Registered ? "connected" : "not-connected"
                })
            };

            return readiness.IsReady
                ? Results.Ok(payload)
                : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
        })
        .WithName("GetPlatformReadiness")
        .AllowAnonymous();
    }
}
