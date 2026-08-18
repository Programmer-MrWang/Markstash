namespace Markstash.Application.Packages;

public interface IMarkstashPackageService
{
    Task<MarkstashPackageExportResult> ExportAsync(
        Stream destination,
        MarkstashPackageExportOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<MarkstashPackageImportResult> ImportAsync(
        Stream source,
        MarkstashPackageImportOptions? options = null,
        CancellationToken cancellationToken = default);
}
