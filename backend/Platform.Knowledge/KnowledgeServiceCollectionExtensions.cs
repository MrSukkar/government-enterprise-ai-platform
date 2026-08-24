using Microsoft.Extensions.DependencyInjection;
using Platform.Knowledge.Retrieval;

namespace Platform.Knowledge;

public static class KnowledgeServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformKnowledgeFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IResultFusionService, DeterministicResultFusionService>();
        services.AddScoped<AuthorizedKnowledgeRetriever>();
        return services;
    }
}
