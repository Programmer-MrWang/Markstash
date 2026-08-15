using Markstash.Application.Preferences;
using Microsoft.Extensions.DependencyInjection;

namespace Markstash.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMarkstashApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPreferencesService, PreferencesService>();
        return services;
    }
}
