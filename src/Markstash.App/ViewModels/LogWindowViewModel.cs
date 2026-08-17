using System.Globalization;
using Markstash.App.Localization;
using Markstash.Application.Abstractions;
using Markstash.Application.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Markstash.App.ViewModels;

public sealed partial class LogWindowViewModel(
    IAppLogReader logReader,
    IDesktopIntegrationService desktopIntegration,
    ILogger<LogWindowViewModel> logger) : ViewModelBase
{
    private IReadOnlyList<AppLogEntry> _allEntries = [];
    private IReadOnlyList<LogEntryRowViewModel> _entries = [];
    private bool _includeCritical = true;
    private bool _includeDebug = true;
    private bool _includeError = true;
    private bool _includeInformation = true;
    private bool _includeTrace = true;
    private bool _includeWarning = true;
    private bool _isLoading;
    private string _searchText = string.Empty;
    private string _statusText = string.Empty;

    public IReadOnlyList<LogEntryRowViewModel> Entries
    {
        get => _entries;
        private set
        {
            if (SetProperty(ref _entries, value))
            {
                OnPropertyChanged(nameof(EntryCountText));
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool IncludeCritical
    {
        get => _includeCritical;
        set => SetFilter(ref _includeCritical, value);
    }

    public bool IncludeError
    {
        get => _includeError;
        set => SetFilter(ref _includeError, value);
    }

    public bool IncludeWarning
    {
        get => _includeWarning;
        set => SetFilter(ref _includeWarning, value);
    }

    public bool IncludeInformation
    {
        get => _includeInformation;
        set => SetFilter(ref _includeInformation, value);
    }

    public bool IncludeDebug
    {
        get => _includeDebug;
        set => SetFilter(ref _includeDebug, value);
    }

    public bool IncludeTrace
    {
        get => _includeTrace;
        set => SetFilter(ref _includeTrace, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string EntryCountText => AppStrings.LogEntryCountFormat
        .Replace("{0}", Entries.Count.ToString(CultureInfo.CurrentCulture), StringComparison.Ordinal)
        .Replace("{1}", _allEntries.Count.ToString(CultureInfo.CurrentCulture), StringComparison.Ordinal);

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        StatusText = AppStrings.LogLoading;
        try
        {
            _allEntries = await logReader
                .ReadLatestAsync(3000, cancellationToken)
                .ConfigureAwait(true);
            ApplyFilter();
            StatusText = string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = string.Empty;
        }
        catch (Exception exception)
        {
            LogLoadFailed(logger, exception);
            StatusText = AppStrings.LogLoadFailed;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public bool TryOpenLogsDirectory()
    {
        try
        {
            desktopIntegration.OpenLogsDirectory();
            return true;
        }
        catch (Exception exception)
        {
            LogOpenDirectoryFailed(logger, exception);
            StatusText = AppStrings.OpenLogsDirectoryFailed;
            return false;
        }
    }

    public void ReportCopied(int count)
    {
        StatusText = AppStrings.LogCopyCompletedFormat.Replace(
            "{0}",
            count.ToString(CultureInfo.CurrentCulture),
            StringComparison.Ordinal);
    }

    public void ReportNothingSelected()
    {
        StatusText = AppStrings.LogNothingSelected;
    }

    public void ReportCopyFailure(Exception exception)
    {
        LogCopyFailed(logger, exception);
        StatusText = AppStrings.LogCopyFailed;
    }

    private void SetFilter(
        ref bool field,
        bool value,
        [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (SetProperty(ref field, value, name))
        {
            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        var searchText = SearchText.Trim();
        Entries = _allEntries
            .Where(IsLevelIncluded)
            .Where(entry => string.IsNullOrEmpty(searchText) ||
                            entry.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                            entry.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                            (entry.Exception?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(entry => new LogEntryRowViewModel(entry))
            .ToArray();
    }

    private bool IsLevelIncluded(AppLogEntry entry) => entry.Level switch
    {
        LogLevel.Critical => IncludeCritical,
        LogLevel.Error => IncludeError,
        LogLevel.Warning => IncludeWarning,
        LogLevel.Information => IncludeInformation,
        LogLevel.Debug => IncludeDebug,
        LogLevel.Trace => IncludeTrace,
        _ => false,
    };

    [LoggerMessage(
        EventId = 3401,
        Level = LogLevel.Error,
        Message = "Unable to load application logs.")]
    private static partial void LogLoadFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 3402,
        Level = LogLevel.Error,
        Message = "Unable to open the application logs directory.")]
    private static partial void LogOpenDirectoryFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 3403,
        Level = LogLevel.Error,
        Message = "Unable to copy selected application logs.")]
    private static partial void LogCopyFailed(ILogger logger, Exception exception);
}
