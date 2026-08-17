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

        ApplyTitleBar(window, extendsContentIntoTitleBar);
        window.Opened -= RefreshTitleBar;
        window.Opened += RefreshTitleBar;
        window.Loaded -= ApplyWindowsMaterial;
        window.Loaded += ApplyWindowsMaterial;
    }

    private static void RefreshTitleBar(object? sender, EventArgs eventArgs)
    {
        if (sender is not FAAppWindow window || !OperatingSystem.IsWindows())
        {
            return;
        }

        ApplyTitleBar(window, window.TitleBar.ExtendsContentIntoTitleBar);
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
        ApplyTitleBar(window, window.TitleBar.ExtendsContentIntoTitleBar);
    }

    private static void ApplyTitleBar(
        FAAppWindow window,
        bool extendsContentIntoTitleBar)
    {
        window.TitleBar.ExtendsContentIntoTitleBar = extendsContentIntoTitleBar;
        window.TitleBar.Height = TitleBarHeight;
        window.TitleBar.BackgroundColor = Colors.Transparent;
        window.TitleBar.InactiveBackgroundColor = Colors.Transparent;
        window.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        window.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
    }
}
