using Markstash.App.ViewModels;
using Markstash.Application;
using Markstash.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Markstash.App.Services;

internal static class ServiceConfiguration
{
    public static ServiceProvider Create()
    {
        var services = new ServiceCollection();

        services.AddMarkstashApplication();
        services.AddMarkstashInfrastructure();
        services.AddSingleton<IThemeService, AvaloniaThemeService>();

        services.AddTransient<HomePageViewModel>();
        services.AddTransient<LibraryPageViewModel>();
        services.AddTransient<SearchPageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<MainViewModel>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
