namespace Markstash.Contracts.Health;

public sealed record HealthResponse(
    string Status,
    string Service,
    string Version,
    string ApiVersion,
    DateTimeOffset CheckedAtUtc,
    DateTimeOffset StartedAtUtc,
    double UptimeSeconds);
