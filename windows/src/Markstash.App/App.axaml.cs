using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Markstash.App.Diagnostics;
using Markstash.App.Hosting;
using Markstash.App.Navigation;
using Markstash.App.Services;
using Markstash.App.ViewModels;
using Markstash.App.Views;
using Markstash.Application.Abstractions;
using Markstash.Application.Diagnostics;
using Markstash.Application.Preferences;
using Markstash.Application.Runtime;
using Markstash.Domain.Preferences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Markstash.App;

public partial class App : Avalonia.Application
{
    public static string DisplayVersion { get; } = GetDisplayVersion();

    private readonly AppStartupOptions _startupOptions;
    private IHost? _host;
    private IControlledApplicationLifetime? _controlledLifetime;
    private EventHandler<UserPreferences>? _preferencesChangedHandler;
    private CancellationTokenRegistration _hostStoppingRegistration;
    private int _shutdownStarted;

    public App()
        : this(AppStartupOptions.Default)
    {
    }

    public App(AppStartupOptions startupOptions)
    {
        _startupOptions = startupOptions;
    }

    public override void Initialize()
    {
        try
        {
            AvaloniaXamlLoader.Load(this);
        }
        catch (Exception exception)
        {
            BootstrapCrashReporter.TryWrite(exception, "Application.Xaml");
            throw;
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            _host = ServiceConfiguration.CreateHost(_startupOptions);
            _host.StartAsync().GetAwaiter().GetResult();

            var services = _host.Services;
            var logger = services.GetRequiredService<ILogger<App>>();
            var preferencesService = services.GetRequiredService<IPreferencesService>();
            var themeService = services.GetRequiredService<IThemeService>();
            var appPaths = services.GetRequiredService<IAppPaths>();
            var platformInfo = services.GetRequiredService<IPlatformInfo>();
            var sessionState = services.GetRequiredService<IAppSessionState>();

            themeService.Apply(preferencesService.Current.Theme);
            _preferencesChangedHandler = (_, preferences) =>
                themeService.Apply(preferences.Theme);
            preferencesService.Changed += _preferencesChangedHandler;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = services.GetRequiredService<MainWindow>();
            }
            else if (ApplicationLifetime is IActivityApplicationLifetime activity)
            {
                activity.MainViewFactory = CreateMainView;
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                singleView.MainView = CreateMainView();
            }

            if (ApplicationLifetime is IControlledApplicationLifetime controlled)
            {
                _controlledLifetime = controlled;
                controlled.Exit += OnLifetimeExit;
                _hostStoppingRegistration = services
                    .GetRequiredService<IHostApplicationLifetime>()
                    .ApplicationStopping
                    .Register(OnHostStopping);
            }

            services.GetRequiredService<AppLifecycleService>().MarkRunning();
            if (logger.IsEnabled(LogLevel.Information))
            {
                LogApplicationStarted(
                    logger,
                    DisplayVersion,
                    platformInfo.OperatingSystem,
                    platformInfo.Architecture,
                    appPaths.RootDirectory);
            }

            if (sessionState.PreviousSessionEndedUnexpectedly)
            {
                LogUncleanSessionDetected(logger);
            }

            if (_startupOptions.UnrecognizedArguments.Count > 0)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    LogUnrecognizedArguments(
                        logger,
                        _startupOptions.UnrecognizedArguments.Count);
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception exception)
        {
            _host?.Services.GetService<AppLifecycleService>()?.MarkFaulted();
            _host?.Services.GetService<ICrashReportWriter>()?
                .TryWrite(exception, "Application.Startup", isTerminating: true);
            BootstrapCrashReporter.TryWrite(exception, "Application.Startup");
            try
            {
                _hostStoppingRegistration.Dispose();
                _hostStoppingRegistration = default;
                _host?.Dispose();
            }
            catch (Exception disposeException)
            {
                BootstrapCrashReporter.TryWrite(disposeException, "Application.Startup.Dispose");
            }

            _host = null;
            throw;
        }
    }

    private static string GetDisplayVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var informationalVersion = assembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var metadataSeparator = informationalVersion.IndexOf('+');
            return metadataSeparator < 0
                ? informationalVersion
                : informationalVersion[..metadataSeparator];
        }

        return assembly?.GetName().Version?.ToString(3) ?? "unknown";
    }

    public async Task ShutdownAsync()
    {
        if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0)
        {
            return;
        }

        var host = Interlocked.Exchange(ref _host, null);
        if (host is null)
        {
            return;
        }

        if (_controlledLifetime is not null)
        {
            _controlledLifetime.Exit -= OnLifetimeExit;
            _controlledLifetime = null;
        }

        _hostStoppingRegistration.Dispose();
        _hostStoppingRegistration = default;

        var preferencesService = host.Services.GetService<IPreferencesService>();
        if (preferencesService is not null && _preferencesChangedHandler is not null)
        {
            preferencesService.Changed -= _preferencesChangedHandler;
            _preferencesChangedHandler = null;
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await host.StopAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (host.Services.GetService<ILogger<App>>() is { } logger)
            {
                LogShutdownTimeout(logger);
            }
        }
        catch (Exception exception)
        {
            host.Services.GetService<AppLifecycleService>()?.MarkFaulted();
            host.Services.GetService<ICrashReportWriter>()?
                .TryWrite(exception, "Application.Shutdown", isTerminating: false);
            if (host.Services.GetService<ILogger<App>>() is { } logger)
            {
                LogShutdownFailure(logger, exception);
            }
        }
        finally
        {
            try
            {
                host.Dispose();
            }
            catch (Exception exception)
            {
                BootstrapCrashReporter.TryWrite(exception, "Application.Shutdown.Dispose");
            }
        }
    }

    public bool TryNavigate(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return _host?.Services.GetService<INavigationService>()?.TryNavigate(uri) ?? false;
    }

    private void OnLifetimeExit(
        object? sender,
        ControlledApplicationLifetimeExitEventArgs eventArgs)
    {
        try
        {
            ShutdownAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            BootstrapCrashReporter.TryWrite(exception, "Application.Exit");
        }
    }

    private void OnHostStopping()
    {
        if (Volatile.Read(ref _shutdownStarted) != 0 ||
            ApplicationLifetime is not IControlledApplicationLifetime controlled)
        {
            return;
        }

        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (Volatile.Read(ref _shutdownStarted) == 0)
                {
                    controlled.Shutdown(0);
                }
            });
        }
        catch (Exception exception)
        {
            BootstrapCrashReporter.TryWrite(exception, "Application.HostStopping");
        }
    }

    private MainView CreateMainView()
    {
        return new MainView
        {
            DataContext = CreateMainViewModel(),
        };
    }

    private MainViewModel CreateMainViewModel()
    {
        return _host?.Services.GetRequiredService<MainViewModel>()
            ?? throw new InvalidOperationException("Application host is not initialized.");
    }

    [LoggerMessage(
        EventId = 3101,
        Level = LogLevel.Information,
        Message = "Markstash {Version} started on {OperatingSystem} ({Architecture}); data root {DataRoot}.")]
    private static partial void LogApplicationStarted(
        ILogger logger,
        string version,
        string operatingSystem,
        string architecture,
        string dataRoot);

    [LoggerMessage(
        EventId = 3102,
        Level = LogLevel.Warning,
        Message = "A marker from an unclean previous session was detected.")]
    private static partial void LogUncleanSessionDetected(ILogger logger);

    [LoggerMessage(
        EventId = 3103,
        Level = LogLevel.Warning,
        Message = "Ignored {ArgumentCount} unrecognized startup argument(s).")]
    private static partial void LogUnrecognizedArguments(
        ILogger logger,
        int argumentCount);

    [LoggerMessage(
        EventId = 3104,
        Level = LogLevel.Warning,
        Message = "Application host shutdown exceeded the five-second timeout.")]
    private static partial void LogShutdownTimeout(ILogger logger);

    [LoggerMessage(
        EventId = 3105,
        Level = LogLevel.Error,
        Message = "Application host shutdown failed.")]
    private static partial void LogShutdownFailure(
        ILogger logger,
        Exception exception);
}
