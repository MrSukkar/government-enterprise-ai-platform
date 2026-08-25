using Microsoft.Extensions.DependencyInjection;
using Platform.AgenticWork.Execution;

namespace Platform.AgenticWork;

public static class AgenticWorkServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformAgenticWorkFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<DurableAgenticWorkEngine>();
        return services;
    }
}
