using Markstash.Application.Abstractions;

namespace Markstash.Infrastructure.Paths;

internal sealed class PlatformAppPaths : IAppPaths
{
    private const string DataDirectoryEnvironmentVariable = "MARKSTASH_DATA_DIR";

    public PlatformAppPaths()
    {
        var configuredRoot = Environment.GetEnvironmentVariable(DataDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            RootDirectory = Path.GetFullPath(configuredRoot);
            DataDirectory = Path.Combine(RootDirectory, "Data");
            ConfigurationDirectory = Path.Combine(RootDirectory, "Config");
            CacheDirectory = Path.Combine(RootDirectory, "Cache");
            TemporaryDirectory = Path.Combine(RootDirectory, "Temp");
            LogsDirectory = Path.Combine(RootDirectory, "Logs");
            CrashReportsDirectory = Path.Combine(LogsDirectory, "Crashes");
            BackupsDirectory = Path.Combine(RootDirectory, "Backups");
            DatabaseDirectory = Path.Combine(RootDirectory, "Database");
            StateDirectory = Path.Combine(RootDirectory, "State");
            PreferencesFile = Path.Combine(ConfigurationDirectory, "preferences.json");
            return;
        }

        RootDirectory = ResolveDataDirectory();
        DataDirectory = Path.Combine(RootDirectory, "Data");
        ConfigurationDirectory = ResolveConfigurationDirectory(RootDirectory);
        CacheDirectory = ResolveCacheDirectory(RootDirectory);
        TemporaryDirectory = Path.Combine(RootDirectory, "Temp");
        LogsDirectory = ResolveStateDirectory(RootDirectory, "Logs");
        CrashReportsDirectory = Path.Combine(LogsDirectory, "Crashes");
        BackupsDirectory = Path.Combine(RootDirectory, "Backups");
        DatabaseDirectory = Path.Combine(RootDirectory, "Database");
        StateDirectory = ResolveStateDirectory(RootDirectory, "State");
        PreferencesFile = Path.Combine(ConfigurationDirectory, "preferences.json");
    }

    public string RootDirectory { get; }

    public string DataDirectory { get; }

    public string ConfigurationDirectory { get; }

    public string CacheDirectory { get; }

    public string TemporaryDirectory { get; }

    public string LogsDirectory { get; }

    public string CrashReportsDirectory { get; }

    public string BackupsDirectory { get; }

    public string DatabaseDirectory { get; }

    public string StateDirectory { get; }

    public string PreferencesFile { get; }

    public void EnsureCreated()
    {
        foreach (var directory in new[]
                 {
                     RootDirectory,
                     DataDirectory,
                     ConfigurationDirectory,
                     CacheDirectory,
                     TemporaryDirectory,
                     LogsDirectory,
                     CrashReportsDirectory,
                     BackupsDirectory,
                     DatabaseDirectory,
                     StateDirectory,
                 })
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string ResolveDataDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "Markstash");
        }

        if (OperatingSystem.IsLinux())
        {
            return Path.Combine(ResolveXdgDirectory("XDG_DATA_HOME", ".local", "share"), "markstash");
        }

        return Path.Combine(ResolveLocalApplicationData(), "Markstash");
    }

    private static string ResolveConfigurationDirectory(string fallbackRoot)
    {
        if (!OperatingSystem.IsLinux())
        {
            return Path.Combine(fallbackRoot, "Config");
        }

        return Path.Combine(ResolveXdgDirectory("XDG_CONFIG_HOME", ".config"), "markstash");
    }

    private static string ResolveCacheDirectory(string fallbackRoot)
    {
        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Caches",
                "Markstash");
        }

        if (OperatingSystem.IsLinux())
        {
            return Path.Combine(ResolveXdgDirectory("XDG_CACHE_HOME", ".cache"), "markstash");
        }

        return Path.Combine(fallbackRoot, "Cache");
    }

    private static string ResolveStateDirectory(string fallbackRoot, string childDirectory)
    {
        if (OperatingSystem.IsMacOS() && childDirectory == "Logs")
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Logs",
                "Markstash");
        }

        if (!OperatingSystem.IsLinux())
        {
            return Path.Combine(fallbackRoot, childDirectory);
        }

        return Path.Combine(
            ResolveXdgDirectory("XDG_STATE_HOME", ".local", "state"),
            "markstash",
            childDirectory);
    }

    private static string ResolveXdgDirectory(string variableName, params string[] fallbackSegments)
    {
        var configured = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrWhiteSpace(configured) || !Path.IsPathRooted(configured)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Path.Combine(fallbackSegments))
            : configured;
    }

    private static string ResolveLocalApplicationData()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(root)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : root;
    }
}
