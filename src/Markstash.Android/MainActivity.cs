using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using MarkstashApplication = Markstash.App.App;

namespace Markstash.Android;

[Activity(
    Label = "Markstash",
    Theme = "@style/MarkstashTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.Orientation |
        ConfigChanges.ScreenSize |
        ConfigChanges.UiMode)]
[IntentFilter(
    new[] { global::Android.Content.Intent.ActionView },
    Categories = new[]
    {
        global::Android.Content.Intent.CategoryDefault,
        global::Android.Content.Intent.CategoryBrowsable,
    },
    DataScheme = "markstash",
    DataHost = "app")]
public sealed class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        NavigateToIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent is not null)
        {
            NavigateToIntent(intent);
        }
    }

    private static void NavigateToIntent(Intent? intent)
    {
        var data = intent?.Data?.ToString();
        if (string.IsNullOrWhiteSpace(data) ||
            !Uri.TryCreate(data, UriKind.Absolute, out var uri) ||
            global::Avalonia.Application.Current is not MarkstashApplication application)
        {
            return;
        }

        application.TryNavigate(uri);
    }
}
