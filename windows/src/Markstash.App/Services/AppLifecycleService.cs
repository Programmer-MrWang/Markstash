using Markstash.Application.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Markstash.App.Services;

internal sealed partial class AppLifecycleService(
    ILogger<AppLifecycleService> logger) : IAppLifecycle, IHostedService, IDisposable
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _stopping = new();
    private AppLifecycleState _state = AppLifecycleState.Created;

    public AppLifecycleState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    public CancellationToken StoppingToken => _stopping.Token;

    public event EventHandler<AppLifecycleChangedEventArgs>? StateChanged;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        TransitionTo(AppLifecycleState.Starting);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        TransitionTo(AppLifecycleState.Stopping);
        try
        {
            _stopping.Cancel();
        }
        catch (Exception exception)
        {
            try
            {
                LogStoppingObserverFailure(logger, exception);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Unable to notify stopping observers: {exception}");
            }
        }

        TransitionTo(AppLifecycleState.Stopped);
        return Task.CompletedTask;
    }

    public void MarkRunning() => TransitionTo(AppLifecycleState.Running);

    public void MarkFaulted() => TransitionTo(AppLifecycleState.Faulted);

    public void Dispose()
    {
        _stopping.Dispose();
    }

    private void TransitionTo(AppLifecycleState next)
    {
        AppLifecycleState previous;
        lock (_gate)
        {
            if (_state == next)
            {
                return;
            }

            previous = _state;
            _state = next;
        }

        var eventArgs = new AppLifecycleChangedEventArgs(previous, next);
        foreach (var handler in StateChanged?.GetInvocationList()
                     .OfType<EventHandler<AppLifecycleChangedEventArgs>>()
                     .ToArray() ?? [])
        {
            try
            {
                handler(this, eventArgs);
            }
            catch (Exception exception)
            {
                try
                {
                    LogStateObserverFailure(logger, exception, previous, next);
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Unable to notify lifecycle observer: {exception}");
                }
            }
        }
    }

    [LoggerMessage(
        EventId = 2201,
        Level = LogLevel.Warning,
        Message = "An application lifecycle observer failed while changing state from {PreviousState} to {NextState}.")]
    private static partial void LogStateObserverFailure(
        ILogger logger,
        Exception exception,
        AppLifecycleState previousState,
        AppLifecycleState nextState);

    [LoggerMessage(
        EventId = 2202,
        Level = LogLevel.Warning,
        Message = "An application stopping observer failed.")]
    private static partial void LogStoppingObserverFailure(
        ILogger logger,
        Exception exception);
}
