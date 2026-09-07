using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Platform.Knowledge.Retrieval;

namespace Platform.Knowledge;

public static class KnowledgeServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformKnowledgeFoundation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var options = configuration.GetSection(Neo4jEnterpriseGraphOptions.SectionName)
            .Get<Neo4jEnterpriseGraphOptions>() ?? new();
        services.Configure<Neo4jEnterpriseGraphOptions>(
            configuration.GetSection(Neo4jEnterpriseGraphOptions.SectionName));
        services.AddSingleton(new Neo4jEnterpriseGraphReadiness(options.ConfigurationState));
        if (options.IsOperationallyConfigured)
        {
            services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(
                options.Uri, AuthTokens.Basic(options.Username, options.Password)));
        }
        services.AddSingleton<IResultFusionService, DeterministicResultFusionService>();
        services.AddScoped<AuthorizedKnowledgeRetriever>();
        return services;
    }
}
