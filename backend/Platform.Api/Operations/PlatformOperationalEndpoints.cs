namespace Platform.Api.Operations;

internal static class PlatformOperationalEndpoints
{
    internal static IEndpointConventionBuilder MapPlatformOperationalReadiness(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGet("/health/ready", (PlatformRuntimeReadiness readiness) =>
        {
            var payload = new
            {
                status = readiness.IsReady ? "ready" : "not-ready",
                failClosed = true,
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
