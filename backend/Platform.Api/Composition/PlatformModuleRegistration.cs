using Platform.Application.Abstractions;

namespace Platform.Api.Composition;

internal static class PlatformModuleRegistration
{
    internal static IServiceCollection AddPlatformModules(
        this IServiceCollection services,
        params IPlatformModule[] modules)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(modules);

        foreach (var module in modules)
        {
            services.AddSingleton(typeof(IPlatformModule), module);
        }

        return services;
    }
}
