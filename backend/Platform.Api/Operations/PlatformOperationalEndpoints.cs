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
            Platform.SoftwareFactory.InternalService.ExistingArchitectureRuntimeReadiness existingArchitecture) =>
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
                    existingArchitecture = existingArchitecture.State.ToString().ToLowerInvariant()
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
