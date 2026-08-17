using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Markstash.App.Localization;
using Markstash.App.ViewModels;
using Markstash.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Markstash.App.Views;

public sealed partial class MainWindow : FAAppWindow
{
    private IDesktopIntegrationService _desktopIntegration = null!;
    private ILogger<MainWindow> _logger = null!;
    private LogWindowViewModel _logWindowViewModel = null!;
    private LogWindow? _logWindow;

    public MainWindow()
    {
        InitializeComponent();
        WindowChromeHelper.ConfigureForWindows(this, extendsContentIntoTitleBar: true);
        CreateShortcutMenuItem.IsVisible = false;
    }

    public MainWindow(
        MainViewModel mainViewModel,
        LogWindowViewModel logWindowViewModel,
        IDesktopIntegrationService desktopIntegration,
        ILogger<MainWindow> logger) : this()
    {
        _logWindowViewModel = logWindowViewModel;
        _desktopIntegration = desktopIntegration;
        _logger = logger;
        DataContext = mainViewModel;
        CreateShortcutMenuItem.IsVisible = desktopIntegration.IsSupported;
    }

    private void OnTitleBarPointerPressed(
        object? sender,
        PointerPressedEventArgs eventArgs)
    {
        if (!OperatingSystem.IsWindows() ||
            TitleBarActions.IsPointerOver ||
            !eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (eventArgs.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            eventArgs.Handled = true;
            return;
        }

        BeginMoveDrag(eventArgs);
        eventArgs.Handled = true;
    }

    private void OnCreateShortcutClick(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            var shortcutPath = _desktopIntegration.CreateDesktopShortcut();
            ShowActionResult(
                AppStrings.ShortcutCreatedTitle,
                AppStrings.ShortcutCreatedMessageFormat.Replace(
                    "{0}",
                    shortcutPath,
                    StringComparison.Ordinal),
                FAInfoBarSeverity.Success);
        }
        catch (Exception exception)
        {
            LogDesktopActionFailed(_logger, AppStrings.MenuCreateShortcut, exception);
            ShowActionFailure(AppStrings.MenuCreateShortcut, exception);
        }
    }

    private void OnLogsClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (_logWindow is not null)
        {
            _logWindow.Activate();
            return;
        }

        var window = new LogWindow(_logWindowViewModel);
        _logWindow = window;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_logWindow, window))
            {
                _logWindow = null;
            }
        };
        window.Show(this);
    }

    private void OnOpenLogsDirectoryClick(object? sender, RoutedEventArgs eventArgs)
    {
        RunDesktopAction(
            AppStrings.MenuOpenLogsDirectory,
            _desktopIntegration.OpenLogsDirectory);
    }

    private void OnOpenApplicationDirectoryClick(object? sender, RoutedEventArgs eventArgs)
    {
        RunDesktopAction(
            AppStrings.MenuOpenApplicationDirectory,
            _desktopIntegration.OpenApplicationDirectory);
    }

    private void RunDesktopAction(string actionName, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            LogDesktopActionFailed(_logger, actionName, exception);
            ShowActionFailure(actionName, exception);
        }
    }

    private void ShowActionFailure(string actionName, Exception exception)
    {
        ShowActionResult(
            AppStrings.ActionFailedTitle,
            AppStrings.ActionFailedMessageFormat
                .Replace("{0}", actionName, StringComparison.Ordinal)
                .Replace("{1}", exception.Message, StringComparison.Ordinal),
            FAInfoBarSeverity.Error);
    }

    private void ShowActionResult(string title, string message, FAInfoBarSeverity severity)
    {
        ActionInfoBar.IsOpen = false;
        ActionInfoBar.Title = title;
        ActionInfoBar.Message = message;
        ActionInfoBar.Severity = severity;
        ActionInfoBar.IsOpen = true;
    }

    [LoggerMessage(
        EventId = 3301,
        Level = LogLevel.Error,
        Message = "Desktop action {ActionName} failed.")]
    private static partial void LogDesktopActionFailed(
        ILogger logger,
        string actionName,
        Exception exception);
}
