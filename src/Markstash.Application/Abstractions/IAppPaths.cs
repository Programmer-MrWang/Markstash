namespace Markstash.Application.Abstractions;

public interface IAppPaths
{
    string RootDirectory { get; }

    string DataDirectory { get; }

    string ConfigurationDirectory { get; }

    string CacheDirectory { get; }

    string TemporaryDirectory { get; }

    string LogsDirectory { get; }

    string CrashReportsDirectory { get; }

    string BackupsDirectory { get; }

    string DatabaseDirectory { get; }

    string StateDirectory { get; }

    string PreferencesFile { get; }

    void EnsureCreated();
}
