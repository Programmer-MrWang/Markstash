using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Windowing;
using Markstash.App.Views;

namespace Markstash.App.Features.Diagnostics;

public partial class LogWindow : FAAppWindow
{
    private LogWindowViewModel _viewModel = null!;

    public LogWindow()
    {
        InitializeComponent();
        WindowChromeHelper.ConfigureForWindows(this, extendsContentIntoTitleBar: false);
    }

    public LogWindow(LogWindowViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs eventArgs)
    {
        await _viewModel.ReloadAsync();
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs eventArgs)
    {
        await _viewModel.ReloadAsync();
    }

    private async void OnCopySelectedClick(object? sender, RoutedEventArgs eventArgs)
    {
        var selectedEntries = LogGrid.SelectedItems
            .OfType<LogEntryRowViewModel>()
            .ToArray();
        if (selectedEntries.Length == 0)
        {
            _viewModel.ReportNothingSelected();
            return;
        }

        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null)
            {
                throw new InvalidOperationException("The clipboard is unavailable.");
            }

            await clipboard.SetTextAsync(string.Join(
                Environment.NewLine,
                selectedEntries.Select(entry => entry.ClipboardText)));
            _viewModel.ReportCopied(selectedEntries.Length);
        }
        catch (Exception exception)
        {
            _viewModel.ReportCopyFailure(exception);
        }
    }

    private void OnOpenDirectoryClick(object? sender, RoutedEventArgs eventArgs)
    {
        _viewModel.TryOpenLogsDirectory();
    }
}
