namespace Markstash.Application.Diagnostics;

public interface IAppDiagnosticsService
{
    Task<string> CreateBundleAsync(
        string? destinationFile = null,
        CancellationToken cancellationToken = default);
}
