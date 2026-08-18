using Markstash.Domain.Resources;

namespace Markstash.Application.Packages;

public sealed record MarkstashPackageExportOptions(
    IReadOnlyCollection<ResourceId>? ResourceIds = null);
