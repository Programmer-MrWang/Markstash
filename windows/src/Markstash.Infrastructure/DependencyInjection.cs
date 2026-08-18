using Markstash.Application.Abstractions;
using Markstash.Application.Diagnostics;
using Markstash.Application.Packages;
using Markstash.Application.Resources;
using Markstash.Application.Runtime;
using Markstash.Infrastructure.Diagnostics;
using Markstash.Infrastructure.Logging;
using Markstash.Infrastructure.Packages;
using Markstash.Infrastructure.Paths;
using Markstash.Infrastructure.Preferences;
using Markstash.Infrastructure.Resources;
using Markstash.Infrastructure.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Markstash.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMarkstashInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppPaths, PlatformAppPaths>();
        services.AddSingleton<IPlatformInfo, RuntimePlatformInfo>();
        services.AddSingleton<IUserPreferencesStore, JsonUserPreferencesStore>();
        services.AddSingleton<IResourceRepository, JsonResourceRepository>();
        services.AddSingleton<IMarkstashPackageService, MarkstashPackageService>();
        services.AddSingleton<IAppDiagnosticsService, AppDiagnosticsService>();
        services.AddSingleton<ICrashReportWriter, JsonCrashReportWriter>();
        services.AddSingleton<IAppLogReader, FileAppLogReader>();
        services.AddSingleton<IDesktopIntegrationService, WindowsDesktopIntegrationService>();

        services.AddSingleton<FileLoggerProvider>();
        services.AddSingleton<ILoggerProvider>(provider =>
            provider.GetRequiredService<FileLoggerProvider>());

        services.AddSingleton<AppSessionStateService>();
        services.AddSingleton<IAppSessionState>(provider =>
            provider.GetRequiredService<AppSessionStateService>());
        services.AddSingleton<IHostedService>(provider =>
            provider.GetRequiredService<AppSessionStateService>());
        return services;
    }
}
