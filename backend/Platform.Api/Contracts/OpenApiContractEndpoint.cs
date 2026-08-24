using System.Reflection;

namespace Platform.Api.Contracts;

internal static class OpenApiContractEndpoint
{
    private const string ResourceName = "Platform.Api.Contracts.openapi.v1.json";

    internal static IEndpointConventionBuilder MapApprovedOpenApiContract(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGet("/openapi/v1.json", () =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded API contract '{ResourceName}' was not found.");
            using var reader = new StreamReader(stream);
            return Results.Text(reader.ReadToEnd(), "application/json");
        })
        .WithName("GetOpenApiContract")
        .AllowAnonymous();
    }
}
