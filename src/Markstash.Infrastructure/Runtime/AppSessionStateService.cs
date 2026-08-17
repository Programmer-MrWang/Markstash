using System.Diagnostics;
using System.Text.Json;
using Markstash.Application.Abstractions;
using Markstash.Application.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Markstash.Infrastructure.Runtime;

internal sealed partial class AppSessionStateService(
    IAppPaths paths,
    IPlatformInfo platformInfo,
    ILogger<AppSessionStateService> logger) : IHostedService, IAppSessionState
{
    private string? _markerFile;

    private string SessionDirectory => Path.Combine(paths.StateDirectory, "Sessions");

    public bool PreviousSessionEndedUnexpectedly { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        StartedAt = DateTimeOffset.UtcNow;

        if (platformInfo.IsMobile)
        {
            return Task.CompletedTask;
        }

        try
        {
            Directory.CreateDirectory(SessionDirectory);
            RemoveStaleMarkers();

            _markerFile = Path.Combine(
                SessionDirectory,
                $"active-{Environment.ProcessId}-{Guid.NewGuid():N}.json");
            var marker = JsonSerializer.Serialize(new SessionMarker(
                StartedAt,
                Environment.ProcessId));
            File.WriteAllText(_markerFile, marker);

            if (PreviousSessionEndedUnexpectedly)
            {
                LogPreviousSessionUnclean(logger);
            }
        }
        catch (Exception exception)
        {
            LogMarkerInitializationFailure(logger, exception);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_markerFile is not null && File.Exists(_markerFile))
            {
                File.Delete(_markerFile);
            }
        }
        catch (Exception exception)
        {
            LogMarkerRemovalFailure(logger, exception);
        }

        return Task.CompletedTask;
    }

    private void RemoveStaleMarkers()
    {
        foreach (var markerFile in Directory.EnumerateFiles(SessionDirectory, "active-*.json"))
        {
            try
            {
                var marker = JsonSerializer.Deserialize<SessionMarker>(
                    File.ReadAllText(markerFile));
                if (marker is not null &&
                    marker.ProcessId != Environment.ProcessId &&
                    IsProcessRunning(marker.ProcessId, marker.StartedAtUtc))
                {
                    continue;
                }
            }
            catch (Exception exception) when (
                exception is JsonException or IOException or UnauthorizedAccessException)
            {
            }

            PreviousSessionEndedUnexpectedly = true;
            TryDelete(markerFile);
        }
    }

    private static bool IsProcessRunning(int processId, DateTimeOffset markerStartedAt)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                return false;
            }

            try
            {
                // A reused PID must not make an old marker look active.
                var processStartedAt = process.StartTime.ToUniversalTime();
                return processStartedAt <= markerStartedAt.UtcDateTime.AddSeconds(5);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return true;
            }
            catch (NotSupportedException)
            {
                return true;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return true;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Warning,
        Message = "The previous Markstash session did not shut down cleanly.")]
    private static partial void LogPreviousSessionUnclean(ILogger logger);

    [LoggerMessage(
        EventId = 2102,
        Level = LogLevel.Warning,
        Message = "Unable to remove the active-session marker.")]
    private static partial void LogMarkerRemovalFailure(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 2103,
        Level = LogLevel.Warning,
        Message = "Unable to initialize active-session diagnostics.")]
    private static partial void LogMarkerInitializationFailure(
        ILogger logger,
        Exception exception);

    private sealed record SessionMarker(
        DateTimeOffset StartedAtUtc,
        int ProcessId);
}
