using Microsoft.Extensions.DependencyInjection;
using Platform.Evidence.Chain;

namespace Platform.Evidence;

public static class EvidenceServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformEvidenceFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<CryptographicEvidenceEngine>();
        return services;
    }
}
