using Markstash.ApiClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Markstash.App.Services;

internal sealed partial class BackendConnectivityService(
    IMarkstashApiClient apiClient,
    ILogger<BackendConnectivityService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var health = await apiClient
                .GetHealthAsync(stoppingToken)
                .ConfigureAwait(false);
            LogBackendConnected(logger, health.Service, health.Version, health.ApiVersion);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogBackendUnavailable(logger, exception);
        }
    }

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Connected to Markstash backend {Service} {Version} using API {ApiVersion}.")]
    private static partial void LogBackendConnected(
        ILogger logger,
        string service,
        string version,
        string apiVersion);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "The Markstash backend is unavailable; the desktop client will continue offline.")]
    private static partial void LogBackendUnavailable(ILogger logger, Exception exception);
}
