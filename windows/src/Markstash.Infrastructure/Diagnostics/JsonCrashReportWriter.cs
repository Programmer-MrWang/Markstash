using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Markstash.Application.Abstractions;
using Markstash.Application.Diagnostics;
using Markstash.Infrastructure.Logging;

namespace Markstash.Infrastructure.Diagnostics;

internal sealed class JsonCrashReportWriter : ICrashReportWriter
{
    private const int ReportRetentionCount = 20;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly IAppPaths _paths;

    public JsonCrashReportWriter(IAppPaths paths)
    {
        _paths = paths;
    }

    public void TryWrite(Exception exception, string source, bool isTerminating)
    {
        string? temporaryPath = null;
        try
        {
            Directory.CreateDirectory(_paths.CrashReportsDirectory);
            var report = new CrashReport(
                DateTimeOffset.UtcNow,
                source,
                isTerminating,
                Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
                RuntimeInformation.FrameworkDescription,
                RuntimeInformation.OSDescription,
                RuntimeInformation.ProcessArchitecture.ToString(),
                Environment.ProcessId,
                LogSanitizer.Sanitize(exception.ToString()));
            var path = Path.Combine(
                _paths.CrashReportsDirectory,
                $"crash-{report.TimestampUtc:yyyyMMdd-HHmmss}-{Environment.ProcessId}-{Guid.NewGuid():N}.json");
            temporaryPath = $"{path}.tmp";

            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.ReadWrite | FileShare.Delete,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, report, SerializerOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: true);

            foreach (var staleReport in Directory
                         .EnumerateFiles(_paths.CrashReportsDirectory, "crash-*.json")
                         .OrderByDescending(File.GetLastWriteTimeUtc)
                         .Skip(ReportRetentionCount))
            {
                File.Delete(staleReport);
            }
        }
        catch (Exception reportException)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to write crash report: {reportException}");
        }
        finally
        {
            try
            {
                if (temporaryPath is not null && File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception cleanupException)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Unable to clean temporary crash reports: {cleanupException}");
            }
        }
    }

    private sealed record CrashReport(
        DateTimeOffset TimestampUtc,
        string Source,
        bool IsTerminating,
        string ApplicationVersion,
        string Framework,
        string OperatingSystem,
        string Architecture,
        int ProcessId,
        string Exception);
}
