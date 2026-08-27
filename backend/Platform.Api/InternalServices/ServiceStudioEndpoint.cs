using Platform.SoftwareFactory.InternalService;

namespace Platform.Api.InternalService;

internal static class InternalServiceEndpoint
{
    internal static IEndpointConventionBuilder MapInternalServiceFoundation(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGet("/api/v1/internal-services/foundation", () =>
            Results.Ok(InternalServiceFoundationCatalog.Current))
            .WithName("GetInternalServiceFoundation")
            .WithTags("Create Internal Service")
            .WithSummary("Read the approved Create Internal Service product foundation.")
            .WithDescription("Public product metadata grants no execution authority.")
            .Produces<InternalServiceFoundation>(StatusCodes.Status200OK)
            .AllowAnonymous();
    }
}
