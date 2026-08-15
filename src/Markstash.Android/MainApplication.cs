using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using MarkstashApplication = Markstash.App.App;

namespace Markstash.Android;

[Application]
public sealed class MainApplication : AvaloniaAndroidApplication<MarkstashApplication>
{
    public MainApplication(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
