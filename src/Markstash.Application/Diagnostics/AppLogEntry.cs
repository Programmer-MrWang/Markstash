using Microsoft.Extensions.Logging;

namespace Markstash.Application.Diagnostics;

public sealed record AppLogEntry(
    DateTimeOffset TimestampUtc,
    LogLevel Level,
    string Category,
    int EventId,
    string Message,
    string? Exception);
