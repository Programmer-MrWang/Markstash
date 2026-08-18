namespace Markstash.Contracts.Diagnostics;

public sealed record DiagnosticLogEntryDto(
    DateTimeOffset TimestampUtc,
    string Level,
    string Category,
    int EventId,
    string Message,
    string? Exception);

public sealed record DiagnosticLogsResponse(
    int RequestedLimit,
    int Count,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<DiagnosticLogEntryDto> Entries);
