namespace Markstash.Application.Packages;

public sealed record MarkstashPackageImportResult(
    int PackageVersion,
    int AddedCount,
    int UpdatedCount);
