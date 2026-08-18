using Markstash.App.Hosting;
using Markstash.App.Localization;
using Markstash.App.Navigation;
using Markstash.App.ViewModels;
using Markstash.App.Views;
using Markstash.ApiClient;
using Markstash.Application;
using Markstash.Application.Runtime;
using Markstash.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FluentAvalonia.UI.Controls;

namespace Markstash.App.Services;

internal static class ServiceConfiguration
{
    public static IHost CreateHost(AppStartupOptions startupOptions)
    {
        startupOptions.ApplyEnvironmentOverrides();

        return Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(startupOptions.Verbose
                    ? LogLevel.Trace
                    : LogLevel.Information);
                logging.AddDebug();
                logging.AddJsonConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffK";
                });
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(startupOptions);
                services.AddMarkstashApiClient(options =>
                {
                    var configuredAddress = Environment.GetEnvironmentVariable(
                        MarkstashApiClientOptions.EnvironmentVariable);
                    if (Uri.TryCreate(configuredAddress, UriKind.Absolute, out var baseAddress))
                    {
                        options.BaseAddress = baseAddress;
                    }
                });
                services.AddMarkstashApplication();
                services.AddMarkstashInfrastructure();
                services.AddSingleton<IThemeService, AvaloniaThemeService>();

                services.AddSingleton(new NavigationRoute(
                    "home",
                    AppStrings.NavigationHome,
                    FASymbol.Home,
                    typeof(HomePageViewModel)));
                services.AddSingleton(new NavigationRoute(
                    "library",
                    AppStrings.NavigationLibrary,
                    FASymbol.Library,
                    typeof(LibraryPageViewModel)));
                services.AddSingleton(new NavigationRoute(
                    "search",
                    AppStrings.NavigationSearch,
                    FASymbol.Find,
                    typeof(SearchPageViewModel)));
                services.AddSingleton(new NavigationRoute(
                    "settings",
                    AppStrings.NavigationSettings,
                    FASymbol.Settings,
                    typeof(SettingsPageViewModel),
                    NavigationPlacement.Footer));
                services.AddSingleton<INavigationService, NavigationService>();

                services.AddSingleton<AppLifecycleService>();
                services.AddSingleton<IAppLifecycle>(provider =>
                    provider.GetRequiredService<AppLifecycleService>());
                services.AddSingleton<IHostedService>(provider =>
                    provider.GetRequiredService<AppLifecycleService>());

                services.AddSingleton<AppExceptionHandler>();
                services.AddSingleton<IHostedService>(provider =>
                    provider.GetRequiredService<AppExceptionHandler>());
                services.AddSingleton<IHostedService, BackendConnectivityService>();

                services.AddTransient<HomePageViewModel>();
                services.AddTransient<LibraryPageViewModel>();
                services.AddTransient<SearchPageViewModel>();
                services.AddTransient<SettingsPageViewModel>();
                services.AddSingleton<LogWindowViewModel>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }
}
