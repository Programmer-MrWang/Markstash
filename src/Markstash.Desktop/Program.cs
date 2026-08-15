using Avalonia;
using MarkstashApplication = Markstash.App.App;

namespace Markstash.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<MarkstashApplication>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
