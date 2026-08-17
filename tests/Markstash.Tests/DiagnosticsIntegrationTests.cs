using System.IO.Compression;
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
