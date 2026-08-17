using Avalonia.Threading;
using Markstash.Application.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Markstash.App.Services;

internal sealed partial class AppExceptionHandler(
    ILogger<AppExceptionHandler> logger,
    ICrashReportWriter crashReportWriter,
    AppLifecycleService lifecycle) : IHostedService
{
    private int _isRecording;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispatcher.UIThread.UnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        return Task.CompletedTask;
    }

    private void OnDispatcherUnhandledException(
        object? sender,
        DispatcherUnhandledExceptionEventArgs eventArgs)
    {
        Record(eventArgs.Exception, "Avalonia.Dispatcher", isTerminating: true);
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
    {
        var exception = eventArgs.ExceptionObject as Exception
            ?? new InvalidOperationException(eventArgs.ExceptionObject?.ToString());
        Record(exception, "AppDomain", eventArgs.IsTerminating);
    }

    private void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs eventArgs)
    {
        Record(eventArgs.Exception, "TaskScheduler", isTerminating: false);
        eventArgs.SetObserved();
    }

    private void Record(Exception exception, string source, bool isTerminating)
    {
        if (Interlocked.Exchange(ref _isRecording, 1) != 0)
        {
            return;
        }

        try
        {
            crashReportWriter.TryWrite(exception, source, isTerminating);
        }
        catch (Exception handlerException)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Unable to write the unhandled exception report: {handlerException}");
        }

        if (isTerminating)
        {
            try
            {
                lifecycle.MarkFaulted();
            }
            catch (Exception lifecycleException)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Unable to update the application lifecycle after an exception: {lifecycleException}");
            }
        }

        try
        {
            LogUnhandledException(logger, exception, source, isTerminating);
        }
        catch (Exception loggingException)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Unable to log the unhandled exception: {loggingException}");
        }
        finally
        {
            Volatile.Write(ref _isRecording, 0);
        }
    }

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Critical,
        Message = "Unhandled exception from {CrashSource}; terminating={IsTerminating}.")]
    private static partial void LogUnhandledException(
        ILogger logger,
        Exception exception,
        string crashSource,
        bool isTerminating);
}
