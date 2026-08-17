namespace Markstash.Application.Diagnostics;

public interface IAppLogReader
{
    Task<IReadOnlyList<AppLogEntry>> ReadLatestAsync(
        int maximumEntries = 2000,
        CancellationToken cancellationToken = default);
}
