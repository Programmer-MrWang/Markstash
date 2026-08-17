using System.IO.Compression;
using System.Globalization;
using System.Reflection;
using Markstash.Application.Abstractions;
using Markstash.Application.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Markstash.Infrastructure.Diagnostics;

internal sealed partial class AppDiagnosticsService(
    IAppPaths paths,
    IPlatformInfo platformInfo,
    ILogger<AppDiagnosticsService> logger) : IAppDiagnosticsService
{
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
            var information = string.Create(
                CultureInfo.InvariantCulture,
                $"""
                 GeneratedUtc: {DateTimeOffset.UtcNow:O}
                 ApplicationVersion: {Assembly.GetEntryAssembly()?.GetName().Version}
                 Framework: {platformInfo.Framework}
                 OperatingSystem: {platformInfo.OperatingSystem}
                 Architecture: {platformInfo.Architecture}
                 IsMobile: {platformInfo.IsMobile}
                 ProcessId: {Environment.ProcessId}
                 """);
            await File.WriteAllTextAsync(
                Path.Combine(stagingDirectory, "environment.txt"),
                information,
                cancellationToken);

            CopyFiles(paths.LogsDirectory, stagingDirectory, "Logs", "*.log");
            CopyFiles(paths.LogsDirectory, stagingDirectory, "Logs", "*.log.gz");
            CopyFiles(paths.LogsDirectory, stagingDirectory, "Logs", "*.jsonl");
            CopyFiles(paths.CrashReportsDirectory, stagingDirectory, "Crashes", "*.json");
            if (File.Exists(paths.PreferencesFile) ||
                File.Exists(paths.PreferencesFile + ".bak"))
            {
                var configDirectory = Path.Combine(stagingDirectory, "Config");
                Directory.CreateDirectory(configDirectory);
                CopyFileBestEffort(
                    paths.PreferencesFile,
                    Path.Combine(configDirectory, "preferences.json"));
                CopyFileBestEffort(
                    paths.PreferencesFile + ".bak",
                    Path.Combine(configDirectory, "preferences.json.bak"));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(finalDestination)!);

            await Task.Run(
                () => ZipFile.CreateFromDirectory(stagingDirectory, temporaryArchive),
                cancellationToken);
            File.Move(temporaryArchive, finalDestination, overwrite: true);
            try
            {
                LogBundleCreated(logger, finalDestination);
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
            try
            {
                if (File.Exists(temporaryArchive))
                {
                    File.Delete(temporaryArchive);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            try
            {
                if (Directory.Exists(stagingDirectory))
                {
                    Directory.Delete(stagingDirectory, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Created diagnostics bundle at {DestinationFile}.")]
    private static partial void LogBundleCreated(
        ILogger logger,
        string destinationFile);

    private static void CopyFiles(
        string sourceDirectory,
        string stagingDirectory,
        string destinationName,
        string pattern)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        var destinationDirectory = Path.Combine(stagingDirectory, destinationName);
        Directory.CreateDirectory(destinationDirectory);
        string[] files;
        try
        {
            files = Directory.EnumerateFiles(sourceDirectory, pattern).ToArray();
        }
        catch (IOException)
        {
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        foreach (var file in files)
        {
            try
            {
                var destination = Path.Combine(destinationDirectory, Path.GetFileName(file));
                using var source = new FileStream(
                    file,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var target = new FileStream(
                    destination,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);
                source.CopyTo(target);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static void CopyFileBestEffort(string source, string destination)
    {
        try
        {
            if (File.Exists(source))
            {
                File.Copy(source, destination, overwrite: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
