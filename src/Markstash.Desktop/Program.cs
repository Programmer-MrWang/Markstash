using Avalonia;
using Markstash.App.Hosting;
using MarkstashApplication = Markstash.App.App;

namespace Markstash.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            return BuildAvaloniaApp(args).StartWithClassicDesktopLifetime(args);
        }
        catch (Exception exception)
        {
            EmergencyCrashReporter.TryWrite(exception);
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() => BuildAvaloniaApp([]);

    public static AppBuilder BuildAvaloniaApp(string[] args)
    {
        var startupOptions = AppStartupOptions.Parse(args);
        return AppBuilder.Configure(() => new MarkstashApplication(startupOptions))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
