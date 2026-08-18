using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Markstash.Application.Abstractions;
using Markstash.Application.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Markstash.Infrastructure.Logging;

internal sealed class FileAppLogReader(IAppPaths paths) : IAppLogReader
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";

    private static readonly JsonSerializerOptions LegacySerializerOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
        };

    public async Task<IReadOnlyList<AppLogEntry>> ReadLatestAsync(
        int maximumEntries = 2000,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumEntries);
        if (!Directory.Exists(paths.LogsDirectory))
        {
            return [];
        }

        var files = EnumerateLogFiles();
        var entries = new List<AppLogEntry>(Math.Min(maximumEntries, 256));
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = maximumEntries - entries.Count;
            if (remaining <= 0)
            {
                break;
            }

            var fileEntries = file.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)
                ? await ReadLegacyJsonFileTailAsync(file, remaining, cancellationToken)
                    .ConfigureAwait(false)
                : await ReadTextFileTailAsync(file, remaining, cancellationToken)
                    .ConfigureAwait(false);
            entries.AddRange(fileEntries);
        }

        return entries
            .OrderByDescending(entry => entry.TimestampUtc)
            .Take(maximumEntries)
            .ToArray();
    }

    private string[] EnumerateLogFiles()
    {
        try
        {
            return Directory
                .EnumerateFiles(paths.LogsDirectory)
                .Where(file =>
                    file.EndsWith(".log", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".log.gz", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
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

    private static async Task<IReadOnlyList<AppLogEntry>> ReadTextFileTailAsync(
        string file,
        int maximumEntries,
        CancellationToken cancellationToken)
    {
        var entries = new Queue<AppLogEntry>(Math.Min(maximumEntries, 256));
        try
        {
            await using var fileStream = new FileStream(
                file,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var contentStream = file.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                ? (Stream)new GZipStream(fileStream, CompressionMode.Decompress)
                : fileStream;
            using var reader = new StreamReader(contentStream, Encoding.UTF8);

            PendingEntry? pending = null;
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                if (TryParseHeader(line, out var nextEntry))
                {
                    Commit(entries, pending, maximumEntries);
                    pending = nextEntry;
                }
                else if (pending is not null)
                {
                    pending.Message.AppendLine();
                    pending.Message.Append(line);
                }
            }

            Commit(entries, pending, maximumEntries);
        }
        catch (InvalidDataException)
        {
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return entries.Reverse().ToArray();
    }

    private static async Task<IReadOnlyList<AppLogEntry>> ReadLegacyJsonFileTailAsync(
        string file,
        int maximumEntries,
        CancellationToken cancellationToken)
    {
        var entries = new Queue<AppLogEntry>(Math.Min(maximumEntries, 256));
        try
        {
            await using var stream = new FileStream(
                file,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                if (!TryParseLegacyJson(line, out var entry))
                {
                    continue;
                }

                Enqueue(entries, entry, maximumEntries);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return entries.Reverse().ToArray();
    }

    private static bool TryParseHeader(string line, out PendingEntry entry)
    {
        entry = null!;
        var firstSeparator = line.IndexOf('|');
        var secondSeparator = firstSeparator < 0 ? -1 : line.IndexOf('|', firstSeparator + 1);
        var thirdSeparator = secondSeparator < 0 ? -1 : line.IndexOf('|', secondSeparator + 1);
        if (firstSeparator <= 0 || secondSeparator <= firstSeparator || thirdSeparator <= secondSeparator)
        {
            return false;
        }

        var timestampText = line[..firstSeparator];
        if (!TryParseTimestamp(timestampText, out var timestamp) ||
            !Enum.TryParse<LogLevel>(
                line[(firstSeparator + 1)..secondSeparator],
                ignoreCase: true,
                out var level))
        {
            return false;
        }

        entry = new PendingEntry(
            timestamp,
            level,
            line[(secondSeparator + 1)..thirdSeparator],
            new StringBuilder(line[(thirdSeparator + 1)..]));
        return true;
    }

    private static bool TryParseTimestamp(string value, out DateTimeOffset timestamp)
    {
        return DateTimeOffset.TryParseExact(
                   value,
                   TimestampFormat,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out timestamp) ||
               DateTimeOffset.TryParse(
                   value,
                   CultureInfo.CurrentCulture,
                   DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                   out timestamp) ||
               DateTimeOffset.TryParse(
                   value,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                   out timestamp);
    }

    private static bool TryParseLegacyJson(string line, out AppLogEntry entry)
    {
        entry = null!;
        try
        {
            var persisted = JsonSerializer.Deserialize<LegacyLogEntry>(
                line,
                LegacySerializerOptions);
            if (persisted is null ||
                !Enum.TryParse<LogLevel>(persisted.Level, ignoreCase: true, out var level))
            {
                return false;
            }

            entry = new AppLogEntry(
                persisted.TimestampUtc,
                level,
                persisted.Category ?? string.Empty,
                persisted.EventId,
                persisted.Message ?? string.Empty,
                persisted.Exception);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void Commit(
        Queue<AppLogEntry> entries,
        PendingEntry? pending,
        int maximumEntries)
    {
        if (pending is null)
        {
            return;
        }

        Enqueue(
            entries,
            new AppLogEntry(
                pending.Timestamp.ToUniversalTime(),
                pending.Level,
                pending.Category,
                EventId: 0,
                pending.Message.ToString(),
                Exception: null),
            maximumEntries);
    }

    private static void Enqueue(
        Queue<AppLogEntry> entries,
        AppLogEntry entry,
        int maximumEntries)
    {
        if (entries.Count == maximumEntries)
        {
            entries.Dequeue();
        }

        entries.Enqueue(entry);
    }

    private sealed record PendingEntry(
        DateTimeOffset Timestamp,
        LogLevel Level,
        string Category,
        StringBuilder Message);

    private sealed record LegacyLogEntry(
        DateTimeOffset TimestampUtc,
        string? Level,
        string? Category,
        int EventId,
        string? Message,
        string? Exception);
}
