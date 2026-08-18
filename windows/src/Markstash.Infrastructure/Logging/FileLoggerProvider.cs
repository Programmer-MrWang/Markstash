using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using Markstash.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Markstash.Infrastructure.Logging;

internal sealed class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private const int LogRetentionDays = 30;
    private const int LogRetentionCount = 64;
    private const long MaxLogFileBytes = 10 * 1024 * 1024;

    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly object _writeGate = new();
    private readonly string _logsDirectory;
    private string? _currentLogFile;
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
    private StreamWriter? _writer;

    public FileLoggerProvider(IAppPaths paths)
    {
        _logsDirectory = paths.LogsDirectory;
        try
        {
            Directory.CreateDirectory(_logsDirectory);
            ProcessPreviousLogs();
            DeleteExpiredLogs();
            PruneArchivedLogs();
            OpenWriter();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to initialize file logging: {exception}");
        }
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new FileLogger(this, name));

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        ArgumentNullException.ThrowIfNull(scopeProvider);
        _scopeProvider = scopeProvider;
    }

    public void Dispose()
    {
        StreamWriter? writer;
        lock (_writeGate)
        {
            writer = _writer;
            _writer = null;
            _currentLogFile = null;
            _loggers.Clear();
        }

        TryDispose(writer);
    }

    internal IDisposable PushScope<TState>(TState state)
        where TState : notnull => _scopeProvider.Push(state);

    internal void Write<TState>(
        string category,
        LogLevel level,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        lock (_writeGate)
        {
            if (_writer is null)
            {
                return;
            }

            try
            {
                if (_writer.BaseStream.Length >= MaxLogFileBytes)
                {
                    RotateWriter();
                    if (_writer is null)
                    {
                        return;
                    }
                }

                var message = BuildMessage(formatter(state, exception), exception);
                var timestamp = DateTimeOffset.Now;
                var line = string.Create(
                    CultureInfo.InvariantCulture,
                    $"{timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}|{level}|{LogSanitizer.Sanitize(category)}|{message}");
                _writer.WriteLine(line);
            }
            catch (Exception writeException)
            {
                System.Diagnostics.Debug.WriteLine($"Unable to write application log: {writeException}");
                var failedWriter = _writer;
                _writer = null;
                _currentLogFile = null;
                TryDispose(failedWriter);
            }
        }
    }

    private string BuildMessage(string message, Exception? exception)
    {
        var scopes = new List<string>();
        _scopeProvider.ForEachScope(
            static (scope, values) =>
                values.Add(LogSanitizer.Sanitize(ConvertValue(scope) ?? string.Empty)),
            scopes);

        var prefix = scopes.Count == 0
            ? string.Empty
            : string.Join(" => ", scopes) + " => ";
        var rendered = exception is null
            ? prefix + message
            : $"{prefix}{message}{Environment.NewLine}{exception}";
        return LogSanitizer.Sanitize(rendered);
    }

    private void OpenWriter()
    {
        var timestamp = DateTime.Now;
        for (var sequence = 1; sequence < int.MaxValue; sequence++)
        {
            var fileName = $"log-{timestamp:yyyy-M-d-HH-mm-ss}-{sequence}.log";
            var path = Path.Combine(_logsDirectory, fileName);
            if (File.Exists(path) || File.Exists(path + ".gz"))
            {
                continue;
            }

            try
            {
                var stream = new FileStream(
                    path,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 4096,
                    FileOptions.SequentialScan);
                _currentLogFile = path;
                _writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
                {
                    AutoFlush = true,
                };
                return;
            }
            catch (IOException) when (File.Exists(path))
            {
            }
        }

        throw new IOException("Unable to allocate a unique application log file name.");
    }

    private void RotateWriter()
    {
        var previousWriter = _writer;
        var previousFile = _currentLogFile;
        _writer = null;
        _currentLogFile = null;
        TryDispose(previousWriter);

        if (previousFile is not null)
        {
            TryCompressLog(previousFile);
        }

        try
        {
            OpenWriter();
            DeleteExpiredLogs();
            PruneArchivedLogs();
        }
        catch (Exception rotationException)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Unable to rotate application log: {rotationException}");
        }
    }

    private void ProcessPreviousLogs()
    {
        foreach (var logFile in EnumerateFiles("log-*.log"))
        {
            TryCompressLog(logFile);
        }
    }

    private static void TryCompressLog(string logFile)
    {
        var archive = $"{logFile}.gz";
        if (File.Exists(archive))
        {
            return;
        }

        var temporaryArchive = $"{archive}.tmp-{Guid.NewGuid():N}";
        try
        {
            using (var source = new FileStream(
                       logFile,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.None,
                       bufferSize: 81920,
                       FileOptions.SequentialScan))
            using (var destination = new FileStream(
                       temporaryArchive,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 81920,
                       FileOptions.WriteThrough))
            {
                using (var compressor = new GZipStream(
                           destination,
                           CompressionLevel.Optimal,
                           leaveOpen: true))
                {
                    source.CopyTo(compressor);
                }

                destination.Flush(flushToDisk: true);
            }

            File.Move(temporaryArchive, archive);
            File.Delete(logFile);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        finally
        {
            TryDelete(temporaryArchive);
        }
    }

    private void DeleteExpiredLogs()
    {
        var threshold = DateTime.UtcNow.AddDays(-LogRetentionDays);
        foreach (var file in EnumerateFiles("log-*.log")
                     .Concat(EnumerateFiles("log-*.log.gz"))
                     .Concat(EnumerateFiles("markstash-*.jsonl")))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < threshold)
                {
                    File.Delete(file);
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

    private void PruneArchivedLogs()
    {
        foreach (var file in EnumerateFiles("log-*.log.gz")
                     .OrderByDescending(File.GetLastWriteTimeUtc)
                     .Skip(LogRetentionCount))
        {
            TryDelete(file);
        }
    }

    private string[] EnumerateFiles(string pattern)
    {
        try
        {
            return Directory.EnumerateFiles(_logsDirectory, pattern).ToArray();
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

    private static void TryDelete(string path)
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

    private static void TryDispose(StreamWriter? writer)
    {
        if (writer is null)
        {
            return;
        }

        try
        {
            writer.Dispose();
        }
        catch (Exception disposeException)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Unable to close application log: {disposeException}");
        }
    }

    private static string? ConvertValue(object? value) => value switch
    {
        null => null,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };
}
