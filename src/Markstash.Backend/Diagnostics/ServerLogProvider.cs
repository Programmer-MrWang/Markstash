using Microsoft.Extensions.Logging;

namespace Markstash.Backend.Diagnostics;

internal sealed class ServerLogProvider(
    ServerLogBuffer buffer,
    TimeProvider timeProvider) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        new ServerLogger(categoryName, buffer, timeProvider);

    public void Dispose()
    {
    }

    private sealed class ServerLogger(
        string category,
        ServerLogBuffer buffer,
        TimeProvider timeProvider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (!IsEnabled(logLevel))
            {
                return;
            }

            buffer.Add(new ServerLogEntry(
                timeProvider.GetUtcNow(),
                logLevel,
                category,
                eventId.Id,
                formatter(state, exception),
                exception?.ToString()));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
