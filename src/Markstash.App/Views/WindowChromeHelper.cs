using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FluentAvalonia.UI.Windowing;

namespace Markstash.App.Views;

internal static class WindowChromeHelper
{
    public const double TitleBarHeight = 48;

    public static void ConfigureForWindows(
        FAAppWindow window,
        bool extendsContentIntoTitleBar)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        window.TitleBar.Height = TitleBarHeight;
        window.TitleBar.ExtendsContentIntoTitleBar = extendsContentIntoTitleBar;
        window.TitleBar.BackgroundColor = Colors.Transparent;
        window.TitleBar.InactiveBackgroundColor = Colors.Transparent;
        window.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        window.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        window.Loaded -= ApplyWindowsMaterial;
        window.Loaded += ApplyWindowsMaterial;
    }

    private static void ApplyWindowsMaterial(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not FAAppWindow window || !OperatingSystem.IsWindows())
        {
            return;
        }

        window.Loaded -= ApplyWindowsMaterial;
        window.TransparencyLevelHint =
        [
            WindowTransparencyLevel.Mica,
            WindowTransparencyLevel.AcrylicBlur,
        ];
        window.Background = Brushes.Transparent;
    }
}
