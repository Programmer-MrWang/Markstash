using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Markstash.Application.Abstractions;
using Markstash.Application.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Markstash.Infrastructure.Diagnostics;

internal sealed partial class AppDiagnosticsService(
    IAppPaths paths,
    IPlatformInfo platformInfo,
    ILogger<AppDiagnosticsService> logger) : IAppDiagnosticsService
{
    private const int BundleFormatVersion = 1;
    private const int MaximumLogFiles = 64;
    private const int MaximumCrashReports = 20;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly DiagnosticContentSanitizer _sanitizer = new(paths);

    public async Task<string> CreateBundleAsync(
        string? destinationFile = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.TemporaryDirectory);
        destinationFile ??= Path.Combine(
            paths.TemporaryDirectory,
            $"markstash-diagnostics-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.zip");
        var finalDestination = Path.GetFullPath(destinationFile);
        var temporaryArchive = $"{finalDestination}.tmp-{Guid.NewGuid():N}";

        var stagingDirectory = Path.Combine(
            paths.TemporaryDirectory,
            $"diagnostics-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDirectory);

        try
        {
            var generatedUtc = DateTimeOffset.UtcNow;
            await WriteEnvironmentAsync(stagingDirectory, generatedUtc, cancellationToken);
            await WritePreferencesSummaryAsync(stagingDirectory, cancellationToken);

            var logCount = CopySanitizedLogs(stagingDirectory, cancellationToken);
            var crashReportCount = CopySanitizedCrashReports(
                stagingDirectory,
                cancellationToken);

            await WriteManifestAsync(
                stagingDirectory,
                generatedUtc,
                logCount,
                crashReportCount,
                cancellationToken);

            Directory.CreateDirectory(Path.GetDirectoryName(finalDestination)!);

            await Task.Run(
                () => ZipFile.CreateFromDirectory(stagingDirectory, temporaryArchive),
                cancellationToken);
            File.Move(temporaryArchive, finalDestination, overwrite: true);
            try
            {
                LogBundleCreated(logger);
            }
            catch (Exception logException)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Unable to log diagnostics bundle creation: {logException}");
            }

            return finalDestination;
        }
        finally
        {
            TryDeleteFile(temporaryArchive);
            TryDeleteDirectory(stagingDirectory);
        }
    }

    private async Task WriteEnvironmentAsync(
        string stagingDirectory,
        DateTimeOffset generatedUtc,
        CancellationToken cancellationToken)
    {
        var applicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? "unknown";
        var information = string.Create(
            CultureInfo.InvariantCulture,
            $"""
             GeneratedUtc: {generatedUtc:O}
             ApplicationVersion: {_sanitizer.Sanitize(applicationVersion)}
             Framework: {_sanitizer.Sanitize(platformInfo.Framework)}
             OperatingSystem: {_sanitizer.Sanitize(platformInfo.OperatingSystem)}
             Architecture: {_sanitizer.Sanitize(platformInfo.Architecture)}
             IsMobile: {platformInfo.IsMobile}
             ProcessId: {Environment.ProcessId}
             """);
        await File.WriteAllTextAsync(
            Path.Combine(stagingDirectory, "environment.txt"),
            information,
            cancellationToken);
    }

    private async Task WritePreferencesSummaryAsync(
        string stagingDirectory,
        CancellationToken cancellationToken)
    {
        var configDirectory = Path.Combine(stagingDirectory, "Config");
        Directory.CreateDirectory(configDirectory);
        var summary = DiagnosticPreferencesSummary.Read(
            paths.PreferencesFile,
            paths.PreferencesFile + ".bak");
        await WriteJsonAsync(
            Path.Combine(configDirectory, "preferences-summary.json"),
            summary,
            cancellationToken);
    }

    private static async Task WriteManifestAsync(
        string stagingDirectory,
        DateTimeOffset generatedUtc,
        int logCount,
        int crashReportCount,
        CancellationToken cancellationToken)
    {
        var manifest = new DiagnosticBundleManifest(
            BundleFormatVersion,
            generatedUtc,
            "allowlist-v1",
            [
                new("manifest", 1, "Fixed bundle metadata only."),
                new("environment", 1, "Runtime metadata with filesystem paths omitted or redacted."),
                new("preferencesSummary", 1, "Theme and safe document status metadata only."),
                new("applicationLogs", logCount, "Top-level application log files, sanitized during export."),
                new("crashReports", crashReportCount, "Allowlisted crash fields, sanitized during export."),
            ],
            [
                "resourceRecords",
                "resourceContent",
                "attachments",
                "accessTokensAndCredentials",
                "absoluteFilesystemPaths",
                "rawPreferencesAndBackups",
                "databases",
                "applicationDataDirectories",
            ]);
        await WriteJsonAsync(
            Path.Combine(stagingDirectory, "manifest.json"),
            manifest,
            cancellationToken);
    }

    private int CopySanitizedLogs(
        string stagingDirectory,
        CancellationToken cancellationToken)
    {
        var sourceFiles = EnumerateAllowlistedFiles(
                paths.LogsDirectory,
                ["log-*.log", "log-*.log.gz", "markstash-*.jsonl"])
            .Take(MaximumLogFiles)
            .ToArray();
        if (sourceFiles.Length == 0)
        {
            return 0;
        }

        var destinationDirectory = Path.Combine(stagingDirectory, "Logs");
        Directory.CreateDirectory(destinationDirectory);
        var copied = 0;

        foreach (var sourceFile in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isCompressed = sourceFile.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
            var extension = isCompressed
                ? ".log.gz"
                : Path.GetExtension(sourceFile).Equals(".jsonl", StringComparison.OrdinalIgnoreCase)
                    ? ".jsonl"
                    : ".log";
            var destination = Path.Combine(
                destinationDirectory,
                $"application-{copied + 1:D3}{extension}");
            if (TryCopySanitizedText(sourceFile, destination, isCompressed, cancellationToken))
            {
                copied++;
            }
        }

        return copied;
    }

    private int CopySanitizedCrashReports(
        string stagingDirectory,
        CancellationToken cancellationToken)
    {
        var sourceFiles = EnumerateAllowlistedFiles(
                paths.CrashReportsDirectory,
                ["crash-*.json"])
            .Take(MaximumCrashReports)
            .ToArray();
        if (sourceFiles.Length == 0)
        {
            return 0;
        }

        var destinationDirectory = Path.Combine(stagingDirectory, "Crashes");
        Directory.CreateDirectory(destinationDirectory);
        var copied = 0;
        foreach (var sourceFile in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var report = DiagnosticCrashReport.Read(sourceFile, _sanitizer);
                if (report is null)
                {
                    continue;
                }

                var destination = Path.Combine(
                    destinationDirectory,
                    $"crash-{copied + 1:D3}.json");
                File.WriteAllText(
                    destination,
                    JsonSerializer.Serialize(report, SerializerOptions));
                copied++;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (JsonException)
            {
            }
        }

        return copied;
    }

    private bool TryCopySanitizedText(
        string source,
        string destination,
        bool isCompressed,
        CancellationToken cancellationToken)
    {
        try
        {
            using var sourceStream = new FileStream(
                source,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using Stream sourceContent = isCompressed
                ? new GZipStream(sourceStream, CompressionMode.Decompress, leaveOpen: false)
                : sourceStream;
            using var reader = new StreamReader(
                sourceContent,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true);

            using var destinationStream = new FileStream(
                destination,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            using Stream destinationContent = isCompressed
                ? new GZipStream(
                    destinationStream,
                    CompressionLevel.Optimal,
                    leaveOpen: false)
                : destinationStream;
            using var writer = new StreamWriter(
                destinationContent,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            while (reader.ReadLine() is { } line)
            {
                cancellationToken.ThrowIfCancellationRequested();
                writer.WriteLine(_sanitizer.Sanitize(line));
            }

            return true;
        }
        catch (IOException)
        {
            TryDeleteFile(destination);
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            TryDeleteFile(destination);
            return false;
        }
    }

    private static string[] EnumerateAllowlistedFiles(
        string sourceDirectory,
        IReadOnlyList<string> patterns)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return [];
        }

        try
        {
            return patterns
                .SelectMany(pattern => Directory.EnumerateFiles(
                    sourceDirectory,
                    pattern,
                    SearchOption.TopDirectoryOnly))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToArray();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static async Task WriteJsonAsync<T>(
        string destination,
        T value,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            destination,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.Asynchronous);
        await JsonSerializer.SerializeAsync(
            stream,
            value,
            SerializerOptions,
            cancellationToken);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Created diagnostics bundle.")]
    private static partial void LogBundleCreated(ILogger logger);

    private sealed record DiagnosticBundleManifest(
        int BundleFormatVersion,
        DateTimeOffset GeneratedUtc,
        string ContentPolicy,
        IReadOnlyList<DiagnosticBundleCategory> IncludedCategories,
        IReadOnlyList<string> ExcludedSensitiveCategories);

    private sealed record DiagnosticBundleCategory(
        string Name,
        int EntryCount,
        string Policy);
}
