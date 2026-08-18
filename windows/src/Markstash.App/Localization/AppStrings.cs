using System.Globalization;
using System.Resources;

namespace Markstash.App.Localization;

public static class AppStrings
{
    private static readonly ResourceManager Resources = new(
        "Markstash.App.Localization.Strings",
        typeof(AppStrings).Assembly);

    public static string AppName => Get(nameof(AppName));

    public static string NavigationHome => Get(nameof(NavigationHome));

    public static string NavigationLibrary => Get(nameof(NavigationLibrary));

    public static string NavigationSearch => Get(nameof(NavigationSearch));

    public static string NavigationSettings => Get(nameof(NavigationSettings));

    public static string HomeSubtitle => Get(nameof(HomeSubtitle));

    public static string HomeFavoritesTitle => Get(nameof(HomeFavoritesTitle));

    public static string HomeFavoritesEmpty => Get(nameof(HomeFavoritesEmpty));

    public static string HomeIndexTitle => Get(nameof(HomeIndexTitle));

    public static string HomeIndexReady => Get(nameof(HomeIndexReady));

    public static string HomeIndexDescription => Get(nameof(HomeIndexDescription));

    public static string RuntimeTitle => Get(nameof(RuntimeTitle));

    public static string LibraryItemCountFormat => Get(nameof(LibraryItemCountFormat));

    public static string LibraryNew => Get(nameof(LibraryNew));

    public static string LibraryEmptyTitle => Get(nameof(LibraryEmptyTitle));

    public static string LibraryEmptyDescription => Get(nameof(LibraryEmptyDescription));

    public static string SearchPlaceholder => Get(nameof(SearchPlaceholder));

    public static string SearchEmptyTitle => Get(nameof(SearchEmptyTitle));

    public static string SearchEmptyDescription => Get(nameof(SearchEmptyDescription));

    public static string SettingsThemeTitle => Get(nameof(SettingsThemeTitle));

    public static string SettingsThemeDescription => Get(nameof(SettingsThemeDescription));

    public static string SettingsDataDirectoryTitle => Get(nameof(SettingsDataDirectoryTitle));

    public static string ThemeSystem => Get(nameof(ThemeSystem));

    public static string ThemeLight => Get(nameof(ThemeLight));

    public static string ThemeDark => Get(nameof(ThemeDark));

    public static string TitleBarMoreOptions => Get(nameof(TitleBarMoreOptions));

    public static string AppVersionToolTip => Get(nameof(AppVersionToolTip));

    public static string MenuCreateShortcut => Get(nameof(MenuCreateShortcut));

    public static string MenuLogs => Get(nameof(MenuLogs));

    public static string MenuOpenLogsDirectory => Get(nameof(MenuOpenLogsDirectory));

    public static string MenuOpenApplicationDirectory => Get(nameof(MenuOpenApplicationDirectory));

    public static string ShortcutCreatedTitle => Get(nameof(ShortcutCreatedTitle));

    public static string ShortcutCreatedMessageFormat => Get(nameof(ShortcutCreatedMessageFormat));

    public static string ActionFailedTitle => Get(nameof(ActionFailedTitle));

    public static string ActionFailedMessageFormat => Get(nameof(ActionFailedMessageFormat));

    public static string LogWindowTitle => Get(nameof(LogWindowTitle));

    public static string LogLevelTrace => Get(nameof(LogLevelTrace));

    public static string LogLevelDebug => Get(nameof(LogLevelDebug));

    public static string LogLevelInformation => Get(nameof(LogLevelInformation));

    public static string LogLevelWarning => Get(nameof(LogLevelWarning));

    public static string LogLevelError => Get(nameof(LogLevelError));

    public static string LogLevelCritical => Get(nameof(LogLevelCritical));

    public static string LogSearchPlaceholder => Get(nameof(LogSearchPlaceholder));

    public static string LogColumnTime => Get(nameof(LogColumnTime));

    public static string LogColumnLevel => Get(nameof(LogColumnLevel));

    public static string LogColumnCategory => Get(nameof(LogColumnCategory));

    public static string LogColumnMessage => Get(nameof(LogColumnMessage));

    public static string LogRefresh => Get(nameof(LogRefresh));

    public static string LogCopySelected => Get(nameof(LogCopySelected));

    public static string LogOpenDirectory => Get(nameof(LogOpenDirectory));

    public static string LogEntryCountFormat => Get(nameof(LogEntryCountFormat));

    public static string LogLoading => Get(nameof(LogLoading));

    public static string LogLoadFailed => Get(nameof(LogLoadFailed));

    public static string OpenLogsDirectoryFailed => Get(nameof(OpenLogsDirectoryFailed));

    public static string LogCopyCompletedFormat => Get(nameof(LogCopyCompletedFormat));

    public static string LogNothingSelected => Get(nameof(LogNothingSelected));

    public static string LogCopyFailed => Get(nameof(LogCopyFailed));

    public static string Get(string name) =>
        Resources.GetString(name, CultureInfo.CurrentUICulture) ?? $"[{name}]";
}
