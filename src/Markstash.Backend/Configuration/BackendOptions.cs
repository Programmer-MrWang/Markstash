namespace Markstash.Backend.Configuration;

public sealed class BackendOptions
{
    public const string SectionName = "Markstash:Backend";

    public string ServiceName { get; set; } = "Markstash Backend";

    public bool ExposeDiagnostics { get; set; }

    public int MaximumLogEntries { get; set; } = 500;

    public int DiagnosticBufferCapacity { get; set; } = 2000;

    public string? TemporaryDirectory { get; set; }
}
