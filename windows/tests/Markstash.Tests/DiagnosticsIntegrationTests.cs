using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Markstash.Application.Abstractions;
using Markstash.Application.Diagnostics;
using Markstash.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Markstash.Tests;

[Collection(EnvironmentVariableTestGroup.Name)]
public sealed partial class DiagnosticsIntegrationTests
{
    [Fact]
    public async Task CrashReportAndDiagnosticsBundleArePortableAndReadable()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Markstash.Tests",
            Guid.NewGuid().ToString("N"));
        var previousValue = Environment.GetEnvironmentVariable("MARKSTASH_DATA_DIR");

        try
        {
            Environment.SetEnvironmentVariable("MARKSTASH_DATA_DIR", root);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMarkstashInfrastructure();
            using var provider = services.BuildServiceProvider();

            var logger = provider.GetRequiredService<ILogger<DiagnosticsIntegrationTests>>();
            if (logger.IsEnabled(LogLevel.Information))
            {
                WriteSensitiveTestLog(logger, "should-not-leak");
            }

            provider.GetRequiredService<ICrashReportWriter>().TryWrite(
                new InvalidOperationException("diagnostic test"),
                "Test",
                isTerminating: false);

            var paths = provider.GetRequiredService<IAppPaths>();
            var logFile = Assert.Single(
                Directory.EnumerateFiles(paths.LogsDirectory, "log-*.log"));
            var logText = ReadSharedText(logFile);
            Assert.DoesNotContain("should-not-leak", logText);
            Assert.Contains("|Information|", logText);
            Assert.Contains("Diagnostic test token=***", logText);

            var readableLogs = await provider
                .GetRequiredService<IAppLogReader>()
                .ReadLatestAsync(cancellationToken: CancellationToken.None);
            var readableEntry = Assert.Single(
                readableLogs,
                entry => entry.Category == typeof(DiagnosticsIntegrationTests).FullName);
            Assert.Equal(LogLevel.Information, readableEntry.Level);
            Assert.DoesNotContain("should-not-leak", readableEntry.Message);

            var crashFile = Assert.Single(
                Directory.EnumerateFiles(paths.CrashReportsDirectory, "crash-*.json"));
            using (var report = JsonDocument.Parse(await File.ReadAllTextAsync(
                       crashFile,
                       CancellationToken.None)))
            {
                Assert.Equal("Test", report.RootElement.GetProperty("source").GetString());
                Assert.Contains(
                    "diagnostic test",
                    report.RootElement.GetProperty("exception").GetString());
            }

            var bundle = await provider
                .GetRequiredService<IAppDiagnosticsService>()
                .CreateBundleAsync(cancellationToken: CancellationToken.None);
            Assert.True(File.Exists(bundle));

            using var archive = ZipFile.OpenRead(bundle);
            Assert.Contains(archive.Entries, entry => entry.FullName == "environment.txt");
            Assert.Contains(
                archive.Entries,
                entry => entry.FullName.StartsWith("Crashes/", StringComparison.Ordinal));
            Assert.Contains(
                archive.Entries,
                entry => entry.FullName.EndsWith(".log", StringComparison.Ordinal));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MARKSTASH_DATA_DIR", previousValue);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PreviousLogIsCompressedAndRemainsReadableOnNextStartup()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Markstash.Tests",
            Guid.NewGuid().ToString("N"));
        var previousValue = Environment.GetEnvironmentVariable("MARKSTASH_DATA_DIR");

        try
        {
            Environment.SetEnvironmentVariable("MARKSTASH_DATA_DIR", root);
            string previousLog;
            using (var firstProvider = CreateInfrastructureProvider())
            {
                var logger = firstProvider.GetRequiredService<ILogger<DiagnosticsIntegrationTests>>();
                WriteArchiveTestLog(logger);
                var paths = firstProvider.GetRequiredService<IAppPaths>();
                previousLog = Assert.Single(
                    Directory.EnumerateFiles(paths.LogsDirectory, "log-*.log"));
            }

            using var secondProvider = CreateInfrastructureProvider();
            _ = secondProvider.GetRequiredService<ILogger<DiagnosticsIntegrationTests>>();
            var archivePath = previousLog + ".gz";
            Assert.False(File.Exists(previousLog));
            Assert.True(File.Exists(archivePath));

            await using (var archiveStream = File.OpenRead(archivePath))
            await using (var decompressor = new GZipStream(
                             archiveStream,
                             CompressionMode.Decompress))
            using (var reader = new StreamReader(decompressor))
            {
                Assert.Contains("archive-test", await reader.ReadToEndAsync());
            }

            var readableLogs = await secondProvider
                .GetRequiredService<IAppLogReader>()
                .ReadLatestAsync(cancellationToken: CancellationToken.None);
            Assert.Contains(readableLogs, entry => entry.Message.Contains(
                "archive-test",
                StringComparison.Ordinal));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MARKSTASH_DATA_DIR", previousValue);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DiagnosticsBundleUsesAnAllowlistAndRedactsPrivateData()
    {
        const string preferencesToken = "PREFERENCES-TOKEN-SENTINEL";
        const string backupToken = "BACKUP-TOKEN-SENTINEL";
        const string resourceContent = "RESOURCE-CONTENT-SENTINEL";
        const string attachmentContent = "ATTACHMENT-CONTENT-SENTINEL";
        const string logToken = "LOG-TOKEN-SENTINEL";
        const string crashToken = "CRASH-TOKEN-SENTINEL";
        var root = Path.Combine(
            Path.GetTempPath(),
            "Markstash.Tests",
            Guid.NewGuid().ToString("N"));
        var previousValue = Environment.GetEnvironmentVariable("MARKSTASH_DATA_DIR");

        try
        {
            Environment.SetEnvironmentVariable("MARKSTASH_DATA_DIR", root);
            using var provider = CreateInfrastructureProvider();
            var paths = provider.GetRequiredService<IAppPaths>();
            paths.EnsureCreated();

            var privateResourcePath = Path.Combine(
                paths.DataDirectory,
                "private",
                "resource.md");
            Directory.CreateDirectory(Path.GetDirectoryName(privateResourcePath)!);
            await File.WriteAllTextAsync(privateResourcePath, resourceContent);
            await File.WriteAllTextAsync(
                Path.Combine(paths.DatabaseDirectory, "private.db"),
                attachmentContent);

            await File.WriteAllTextAsync(
                paths.PreferencesFile,
                JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    revision = 7,
                    writtenAtUtc = DateTimeOffset.UtcNow,
                    preferences = new
                    {
                        theme = "Dark",
                        accessToken = preferencesToken,
                        lastResourcePath = privateResourcePath,
                    },
                    resourceContent,
                }));
            await File.WriteAllTextAsync(
                paths.PreferencesFile + ".bak",
                JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    revision = 6,
                    writtenAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                    preferences = new
                    {
                        theme = "Light",
                        accessToken = backupToken,
                    },
                }));

            await File.WriteAllTextAsync(
                Path.Combine(paths.LogsDirectory, "log-privacy.log"),
                $"token={logToken} resourcePath={privateResourcePath}");
            await File.WriteAllTextAsync(
                Path.Combine(paths.CrashReportsDirectory, "crash-privacy.json"),
                JsonSerializer.Serialize(new
                {
                    timestampUtc = DateTimeOffset.UtcNow,
                    source = "PrivacyTest",
                    isTerminating = false,
                    applicationVersion = "1.0.0",
                    framework = ".NET",
                    operatingSystem = "TestOS",
                    architecture = "x64",
                    processId = Environment.ProcessId,
                    exception = $"authorization=Bearer {crashToken} path={privateResourcePath}",
                    accessToken = crashToken,
                    resourceContent,
                }));

            var bundle = await provider
                .GetRequiredService<IAppDiagnosticsService>()
                .CreateBundleAsync(cancellationToken: CancellationToken.None);

            using var archive = ZipFile.OpenRead(bundle);
            Assert.Contains(archive.Entries, entry => entry.FullName == "manifest.json");
            Assert.Contains(
                archive.Entries,
                entry => entry.FullName == "Config/preferences-summary.json");
            Assert.DoesNotContain(
                archive.Entries,
                entry => entry.FullName.Equals(
                    "Config/preferences.json",
                    StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(
                archive.Entries,
                entry => entry.FullName.Contains("Database", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(
                archive.Entries,
                entry => entry.FullName.Contains("Data/", StringComparison.OrdinalIgnoreCase));

            using (var summary = JsonDocument.Parse(await ReadArchiveEntryTextAsync(
                       archive,
                       "Config/preferences-summary.json")))
            {
                Assert.Equal("Dark", summary.RootElement.GetProperty("theme").GetString());
                Assert.Equal(1, summary.RootElement.GetProperty("schemaVersion").GetInt32());
                Assert.Equal(
                    "PrimaryAvailable",
                    summary.RootElement.GetProperty("documentStatus").GetString());
            }

            using (var manifest = JsonDocument.Parse(await ReadArchiveEntryTextAsync(
                       archive,
                       "manifest.json")))
            {
                Assert.Equal(
                    "allowlist-v1",
                    manifest.RootElement.GetProperty("contentPolicy").GetString());
                Assert.Contains(
                    manifest.RootElement
                        .GetProperty("excludedSensitiveCategories")
                        .EnumerateArray()
                        .Select(element => element.GetString()),
                    category => category == "rawPreferencesAndBackups");
                Assert.Contains(
                    manifest.RootElement
                        .GetProperty("excludedSensitiveCategories")
                        .EnumerateArray()
                        .Select(element => element.GetString()),
                    category => category == "resourceContent");
            }

            var bundleText = await ReadAllTextEntriesAsync(archive);
            Assert.DoesNotContain(preferencesToken, bundleText, StringComparison.Ordinal);
            Assert.DoesNotContain(backupToken, bundleText, StringComparison.Ordinal);
            Assert.DoesNotContain(resourceContent, bundleText, StringComparison.Ordinal);
            Assert.DoesNotContain(attachmentContent, bundleText, StringComparison.Ordinal);
            Assert.DoesNotContain(logToken, bundleText, StringComparison.Ordinal);
            Assert.DoesNotContain(crashToken, bundleText, StringComparison.Ordinal);
            Assert.DoesNotContain(root, bundleText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                privateResourcePath.Replace('\\', '/'),
                bundleText,
                StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Dark", bundleText, StringComparison.Ordinal);
            Assert.Contains("***", bundleText, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MARKSTASH_DATA_DIR", previousValue);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static ServiceProvider CreateInfrastructureProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarkstashInfrastructure();
        return services.BuildServiceProvider();
    }

    private static string ReadSharedText(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task<string> ReadArchiveEntryTextAsync(
        ZipArchive archive,
        string entryName)
    {
        var entry = Assert.Single(
            archive.Entries,
            candidate => candidate.FullName == entryName);
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static async Task<string> ReadAllTextEntriesAsync(ZipArchive archive)
    {
        var text = new StringBuilder();
        foreach (var entry in archive.Entries.Where(entry =>
                     entry.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                     entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                     entry.FullName.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase) ||
                     entry.FullName.EndsWith(".log", StringComparison.OrdinalIgnoreCase) ||
                     entry.FullName.EndsWith(".log.gz", StringComparison.OrdinalIgnoreCase)))
        {
            await using var entryStream = entry.Open();
            if (entry.FullName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                await using var decompressor = new GZipStream(
                    entryStream,
                    CompressionMode.Decompress);
                using var compressedReader = new StreamReader(decompressor);
                text.AppendLine(await compressedReader.ReadToEndAsync());
                continue;
            }

            using var reader = new StreamReader(entryStream);
            text.AppendLine(await reader.ReadToEndAsync());
        }

        return text.ToString();
    }

    [LoggerMessage(
        EventId = 9001,
        Level = LogLevel.Information,
        Message = "Diagnostic test token={Token}.")]
    private static partial void WriteSensitiveTestLog(
        ILogger logger,
        string token);

    [LoggerMessage(
        EventId = 9002,
        Level = LogLevel.Warning,
        Message = "archive-test")]
    private static partial void WriteArchiveTestLog(ILogger logger);
}
