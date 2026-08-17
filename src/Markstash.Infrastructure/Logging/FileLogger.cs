using Microsoft.Extensions.Logging;

namespace Markstash.Infrastructure.Logging;

internal sealed class FileLogger(
    FileLoggerProvider provider,
    string categoryName) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => provider.PushScope(state);

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        provider.Write(categoryName, logLevel, eventId, state, exception, formatter);
    }
}
