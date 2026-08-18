namespace Markstash.Backend.Diagnostics;

internal interface IServerDiagnosticsService
{
    IReadOnlyList<ServerLogEntry> ReadLatest(int maximumEntries);

    Task<string> CreateBundleAsync(CancellationToken cancellationToken = default);
}
