using System.Globalization;
using Avalonia.Media;
using Markstash.App.Localization;
using Markstash.Application.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Markstash.App.ViewModels;

public sealed class LogEntryRowViewModel(AppLogEntry entry)
{
    private static readonly IBrush CriticalBackground = new SolidColorBrush(Color.Parse("#44C42B1C"));
    private static readonly IBrush ErrorBackground = new SolidColorBrush(Color.Parse("#38D13438"));
    private static readonly IBrush WarningBackground = new SolidColorBrush(Color.Parse("#38F7B500"));
    private static readonly IBrush InformationBackground = new SolidColorBrush(Color.Parse("#284C8BF5"));
    private static readonly IBrush SubtleBackground = new SolidColorBrush(Color.Parse("#18808080"));

    public AppLogEntry Entry { get; } = entry;

    public string TimestampText =>
        Entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);

    public string LevelText => Entry.Level switch
    {
        LogLevel.Trace => AppStrings.LogLevelTrace,
        LogLevel.Debug => AppStrings.LogLevelDebug,
        LogLevel.Information => AppStrings.LogLevelInformation,
        LogLevel.Warning => AppStrings.LogLevelWarning,
        LogLevel.Error => AppStrings.LogLevelError,
        LogLevel.Critical => AppStrings.LogLevelCritical,
        _ => Entry.Level.ToString(),
    };

    public IBrush LevelBackground => Entry.Level switch
    {
        LogLevel.Critical => CriticalBackground,
        LogLevel.Error => ErrorBackground,
        LogLevel.Warning => WarningBackground,
        LogLevel.Information => InformationBackground,
        _ => SubtleBackground,
    };

    public string Category => Entry.Category;

    public string Message => string.IsNullOrWhiteSpace(Entry.Exception)
        ? Entry.Message
        : $"{Entry.Message}{Environment.NewLine}{Entry.Exception}";

    public string ClipboardText =>
        $"[{TimestampText}] [{Entry.Level}] {Category}: {Message}";
}
