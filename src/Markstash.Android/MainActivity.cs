using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace Markstash.Android;

[Activity(
    Label = "Markstash",
    Theme = "@style/MarkstashTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges =
        ConfigChanges.Orientation |
        ConfigChanges.ScreenSize |
        ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity;
