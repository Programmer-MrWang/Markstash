using Markstash.Application.Abstractions;
using Markstash.Infrastructure.Paths;
using Markstash.Infrastructure.Preferences;
using Microsoft.Extensions.DependencyInjection;

namespace Markstash.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMarkstashInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppPaths, PlatformAppPaths>();
        services.AddSingleton<IUserPreferencesStore, JsonUserPreferencesStore>();
        return services;
    }
}
