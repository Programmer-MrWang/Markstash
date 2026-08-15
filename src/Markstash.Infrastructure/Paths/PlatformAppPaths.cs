using Markstash.Application.Abstractions;

namespace Markstash.Infrastructure.Paths;

internal sealed class PlatformAppPaths : IAppPaths
{
    private const string DataDirectoryEnvironmentVariable = "MARKSTASH_DATA_DIR";

    public PlatformAppPaths()
    {
        DataDirectory = ResolveDataDirectory();
        PreferencesFile = Path.Combine(DataDirectory, "preferences.json");
    }

    public string DataDirectory { get; }

    public string PreferencesFile { get; }

    private static string ResolveDataDirectory()
    {
        var configuredPath = Environment.GetEnvironmentVariable(DataDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

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
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            var linuxDataRoot = string.IsNullOrWhiteSpace(xdgDataHome)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local",
                    "share")
                : xdgDataHome;

            return Path.Combine(linuxDataRoot, "markstash");
        }

        var localDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localDataRoot))
        {
            localDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(localDataRoot, "Markstash");
    }
}
