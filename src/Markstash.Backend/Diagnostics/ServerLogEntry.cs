using Microsoft.Extensions.Logging;

namespace Markstash.Backend.Diagnostics;

internal sealed record ServerLogEntry(
    DateTimeOffset TimestampUtc,
    LogLevel Level,
    string Category,
    int EventId,
    string Message,
    string? Exception);
