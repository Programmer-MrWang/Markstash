using System.Text.Json;
using Markstash.Application;
using Markstash.Application.Abstractions;
using Markstash.Application.Preferences;
using Markstash.Domain.Preferences;
using Markstash.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Markstash.Tests;

[Collection(EnvironmentVariableTestGroup.Name)]
public sealed class JsonPreferencesIntegrationTests
{
    [Fact]
    public void PreferencesRoundTripThroughTheConfiguredDataDirectory()
    {
        using var dataDirectory = new DataDirectoryScope();

        using (var firstProvider = CreateProvider())
        {
            var preferences = firstProvider.GetRequiredService<IPreferencesService>();
            Assert.Equal(PreferencesLoadStatus.Default, preferences.LoadStatus);
            Assert.True(preferences.SetTheme(ThemePreference.Dark));
        }

        using var secondProvider = CreateProvider();
        var reloaded = secondProvider.GetRequiredService<IPreferencesService>();

        Assert.Equal(ThemePreference.Dark, reloaded.Current.Theme);
        Assert.Equal(PreferencesLoadStatus.Loaded, reloaded.LoadStatus);
        Assert.True(File.Exists(dataDirectory.PreferencesFile));
    }

    [Fact]
    public void UnversionedPreferencesAreMigratedToSchemaVersionOne()
    {
        using var dataDirectory = new DataDirectoryScope();
        Directory.CreateDirectory(dataDirectory.ConfigurationDirectory);
        File.WriteAllText(
            dataDirectory.PreferencesFile,
            """
            {
              "theme": "Dark"
            }
            """);

        using var provider = CreateProvider();
        var preferences = provider.GetRequiredService<IPreferencesService>();

        Assert.Equal(ThemePreference.Dark, preferences.Current.Theme);
        Assert.Equal(PreferencesLoadStatus.Migrated, preferences.LoadStatus);

        using var migratedDocument = JsonDocument.Parse(
            File.ReadAllText(dataDirectory.PreferencesFile));
        Assert.Equal(
            1,
            migratedDocument.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(
            "Dark",
            migratedDocument.RootElement
                .GetProperty("preferences")
                .GetProperty("theme")
                .GetString());
    }

    [Fact]
    public void CorruptPrimaryPreferencesAreRecoveredFromBackup()
    {
        using var dataDirectory = new DataDirectoryScope();

        using (var firstProvider = CreateProvider())
        {
            var preferences = firstProvider.GetRequiredService<IPreferencesService>();
            Assert.True(preferences.SetTheme(ThemePreference.Light));
            Assert.True(preferences.SetTheme(ThemePreference.Dark));
        }

        Assert.True(File.Exists(dataDirectory.PreferencesFile + ".bak"));
        File.WriteAllText(dataDirectory.PreferencesFile, "{ not valid json");

        using var secondProvider = CreateProvider();
        var recovered = secondProvider.GetRequiredService<IPreferencesService>();

        Assert.Equal(ThemePreference.Light, recovered.Current.Theme);
        Assert.Equal(PreferencesLoadStatus.RecoveredFromBackup, recovered.LoadStatus);
        Assert.Single(
            Directory.EnumerateFiles(
                Path.Combine(dataDirectory.ConfigurationDirectory, "Corrupt"),
                "preferences.json.*.corrupt"));

        using var recoveredDocument = JsonDocument.Parse(
            File.ReadAllText(dataDirectory.PreferencesFile));
        Assert.Equal(
            "Light",
            recoveredDocument.RootElement
                .GetProperty("preferences")
                .GetProperty("theme")
                .GetString());
    }

    [Fact]
    public void FutureSchemaIsReportedWithoutOverwritingTheFile()
    {
        using var dataDirectory = new DataDirectoryScope();
        Directory.CreateDirectory(dataDirectory.ConfigurationDirectory);
        const string futureDocument = """
            {
              "schemaVersion": 999,
              "revision": 27,
              "writtenAtUtc": "2026-08-16T00:00:00Z",
              "preferences": {
                "theme": "Dark"
              }
            }
            """;
        File.WriteAllText(dataDirectory.PreferencesFile, futureDocument);

        using var provider = CreateProvider();
        var preferences = provider.GetRequiredService<IPreferencesService>();

        Assert.Equal(UserPreferences.Default, preferences.Current);
        Assert.Equal(PreferencesLoadStatus.UnsupportedVersion, preferences.LoadStatus);
        Assert.False(preferences.IsWritable);
        Assert.Contains("999", preferences.LastPersistenceError);
        Assert.False(preferences.SetTheme(ThemePreference.Dark));
        Assert.Equal(futureDocument, File.ReadAllText(dataDirectory.PreferencesFile));
        Assert.False(File.Exists(dataDirectory.PreferencesFile + ".bak"));
    }

    [Fact]
    public void MissingPrimaryIsRecoveredFromBackup()
    {
        using var dataDirectory = new DataDirectoryScope();
        Directory.CreateDirectory(dataDirectory.ConfigurationDirectory);
        File.WriteAllText(
            dataDirectory.PreferencesFile + ".bak",
            """
            {
              "schemaVersion": 1,
              "revision": 4,
              "writtenAtUtc": "2026-08-16T00:00:00Z",
              "preferences": {
                "theme": "Dark"
              }
            }
            """);

        using var provider = CreateProvider();
        var preferences = provider.GetRequiredService<IPreferencesService>();

        Assert.Equal(ThemePreference.Dark, preferences.Current.Theme);
        Assert.Equal(PreferencesLoadStatus.RecoveredFromBackup, preferences.LoadStatus);
        Assert.True(preferences.IsWritable);
        Assert.True(File.Exists(dataDirectory.PreferencesFile));

        using var restoredDocument = JsonDocument.Parse(
            File.ReadAllText(dataDirectory.PreferencesFile));
        Assert.Equal(
            5,
            restoredDocument.RootElement.GetProperty("revision").GetInt64());
    }

    [Fact]
    public void FutureSchemaBackupKeepsTheStoreReadOnly()
    {
        using var dataDirectory = new DataDirectoryScope();
        Directory.CreateDirectory(dataDirectory.ConfigurationDirectory);
        var backupFile = dataDirectory.PreferencesFile + ".bak";
        const string futureDocument = """
            {
              "schemaVersion": 999,
              "revision": 27,
              "writtenAtUtc": "2026-08-16T00:00:00Z",
              "preferences": {
                "theme": "Dark"
              }
            }
            """;
        File.WriteAllText(backupFile, futureDocument);
        File.WriteAllText(dataDirectory.PreferencesFile, "{ invalid json");

        using var provider = CreateProvider();
        var preferences = provider.GetRequiredService<IPreferencesService>();

        Assert.Equal(PreferencesLoadStatus.UnsupportedVersion, preferences.LoadStatus);
        Assert.False(preferences.IsWritable);
        Assert.False(preferences.SetTheme(ThemePreference.Dark));
        Assert.Equal(futureDocument, File.ReadAllText(backupFile));
        Assert.False(File.Exists(dataDirectory.PreferencesFile));
    }

    [Fact]
    public void SavingDoesNotEraseAFutureSchemaBackup()
    {
        using var dataDirectory = new DataDirectoryScope();
        Directory.CreateDirectory(dataDirectory.ConfigurationDirectory);
        using (var initialProvider = CreateProvider())
        {
            Assert.True(initialProvider
                .GetRequiredService<IPreferencesService>()
                .SetTheme(ThemePreference.Light));
        }

        const string futureDocument = """
            {
              "schemaVersion": 999,
              "revision": 27,
              "writtenAtUtc": "2026-08-16T00:00:00Z",
              "preferences": {
                "theme": "Dark"
              }
            }
        """;
        File.WriteAllText(dataDirectory.PreferencesFile + ".bak", futureDocument);

        using var provider = CreateProvider();
        var preferences = provider.GetRequiredService<IPreferencesService>();

        Assert.True(preferences.SetTheme(ThemePreference.Dark));
        Assert.Equal(futureDocument, File.ReadAllText(dataDirectory.PreferencesFile + ".bak"));
    }

    [Fact]
    public void RevisionOverflowFailsWithoutOverwritingPreferences()
    {
        using var dataDirectory = new DataDirectoryScope();
        Directory.CreateDirectory(dataDirectory.ConfigurationDirectory);
        const string document = """
            {
              "schemaVersion": 1,
              "revision": 9223372036854775807,
              "writtenAtUtc": "2026-08-16T00:00:00Z",
              "preferences": {
                "theme": "System"
              }
            }
            """;
        File.WriteAllText(dataDirectory.PreferencesFile, document);

        using var provider = CreateProvider();
        var preferences = provider.GetRequiredService<IPreferencesService>();

        Assert.False(preferences.SetTheme(ThemePreference.Dark));
        Assert.Equal(document, File.ReadAllText(dataDirectory.PreferencesFile));
        Assert.Contains("maximum", preferences.LastPersistenceError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VersionedPropertyNamesAreReadCaseInsensitively()
    {
        using var dataDirectory = new DataDirectoryScope();
        Directory.CreateDirectory(dataDirectory.ConfigurationDirectory);
        File.WriteAllText(
            dataDirectory.PreferencesFile,
            """
            {
              "SchemaVersion": 1,
              "Revision": 2,
              "WrittenAtUtc": "2026-08-16T00:00:00Z",
              "Preferences": {
                "Theme": "Dark"
              }
            }
            """);

        using var provider = CreateProvider();
        var preferences = provider.GetRequiredService<IPreferencesService>();

        Assert.Equal(ThemePreference.Dark, preferences.Current.Theme);
        Assert.Equal(PreferencesLoadStatus.Loaded, preferences.LoadStatus);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData(
        "{\"schemaVersion\":1,\"revision\":1,\"writtenAtUtc\":\"2026-08-16T00:00:00Z\",\"preferences\":null}")]
    [InlineData(
        "{\"schemaVersion\":1,\"SchemaVersion\":999,\"revision\":1,\"writtenAtUtc\":\"2026-08-16T00:00:00Z\",\"preferences\":{\"theme\":\"Dark\"}}")]
    public void MalformedEnvelopesAreIsolatedInsteadOfEscapingStartup(string document)
    {
        using var dataDirectory = new DataDirectoryScope();
        Directory.CreateDirectory(dataDirectory.ConfigurationDirectory);
        File.WriteAllText(dataDirectory.PreferencesFile, document);

        using var provider = CreateProvider();
        var preferences = provider.GetRequiredService<IPreferencesService>();

        Assert.Equal(UserPreferences.Default, preferences.Current);
        Assert.Equal(PreferencesLoadStatus.ResetAfterFailure, preferences.LoadStatus);
        Assert.True(preferences.IsWritable);
        Assert.False(File.Exists(dataDirectory.PreferencesFile));
        Assert.Single(
            Directory.EnumerateFiles(
                Path.Combine(dataDirectory.ConfigurationDirectory, "Corrupt"),
                "preferences.json.*.corrupt"));
    }

    [Fact]
    public void FutureSchemaAtTheLegacyLocationIsKeptReadOnly()
    {
        using var dataDirectory = new DataDirectoryScope();
        Directory.CreateDirectory(dataDirectory.RootDirectory);
        var legacyFile = Path.Combine(dataDirectory.RootDirectory, "preferences.json");
        const string futureDocument = """
            {
              "schemaVersion": 999,
              "revision": 27,
              "writtenAtUtc": "2026-08-16T00:00:00Z",
              "preferences": {
                "theme": "Dark"
              }
            }
            """;
        File.WriteAllText(legacyFile, futureDocument);

        using var provider = CreateProvider();
        var preferences = provider.GetRequiredService<IPreferencesService>();

        Assert.Equal(PreferencesLoadStatus.UnsupportedVersion, preferences.LoadStatus);
        Assert.False(preferences.IsWritable);
        Assert.False(preferences.SetTheme(ThemePreference.Dark));
        Assert.Equal(futureDocument, File.ReadAllText(legacyFile));
        Assert.False(File.Exists(dataDirectory.PreferencesFile));
    }

    [Fact]
    public void DataDirectoryOverrideControlsEveryPathAndDirectoriesCanBeCreated()
    {
        using var dataDirectory = new DataDirectoryScope();
        using var provider = CreateProvider();
        var paths = provider.GetRequiredService<IAppPaths>();

        Assert.Equal(Path.GetFullPath(dataDirectory.RootDirectory), paths.RootDirectory);
        Assert.Equal(dataDirectory.PreferencesFile, paths.PreferencesFile);

        paths.EnsureCreated();

        Assert.All(
            new[]
            {
                paths.RootDirectory,
                paths.DataDirectory,
                paths.ConfigurationDirectory,
                paths.CacheDirectory,
                paths.TemporaryDirectory,
                paths.LogsDirectory,
                paths.CrashReportsDirectory,
                paths.BackupsDirectory,
                paths.DatabaseDirectory,
                paths.StateDirectory,
            },
            directory => Assert.True(
                Directory.Exists(directory),
                $"Expected directory to exist: {directory}"));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddMarkstashApplication();
        services.AddMarkstashInfrastructure();
        return services.BuildServiceProvider();
    }

    private sealed class DataDirectoryScope : IDisposable
    {
        private const string VariableName = "MARKSTASH_DATA_DIR";
        private readonly string? _previousValue;

        public DataDirectoryScope()
        {
            RootDirectory = Path.Combine(
                Path.GetTempPath(),
                "Markstash.Tests",
                Guid.NewGuid().ToString("N"));
            _previousValue = Environment.GetEnvironmentVariable(VariableName);
            Environment.SetEnvironmentVariable(VariableName, RootDirectory);
        }

        public string RootDirectory { get; }

        public string ConfigurationDirectory =>
            Path.Combine(RootDirectory, "Config");

        public string PreferencesFile =>
            Path.Combine(ConfigurationDirectory, "preferences.json");

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(VariableName, _previousValue);

            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
