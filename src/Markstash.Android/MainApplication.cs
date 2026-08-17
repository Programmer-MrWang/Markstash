using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Markstash.App.Diagnostics;
using MarkstashApplication = Markstash.App.App;

namespace Markstash.Android;

[Application]
public sealed class MainApplication : AvaloniaAndroidApplication<MarkstashApplication>
{
    public MainApplication(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    public override void OnCreate()
    {
        AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidUnhandledException;
        try
        {
            base.OnCreate();
        }
        catch (Exception exception)
        {
            BootstrapCrashReporter.TryWrite(exception, "Android.Application");
            throw;
        }
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    private static void OnAndroidUnhandledException(
        object? sender,
        RaiseThrowableEventArgs eventArgs)
    {
        BootstrapCrashReporter.TryWrite(eventArgs.Exception, "Android.Runtime");
    }
}
