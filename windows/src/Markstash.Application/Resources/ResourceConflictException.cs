using Markstash.Domain.Resources;

namespace Markstash.Application.Resources;

public sealed class ResourceConflictException(IReadOnlyList<ResourceId> resourceIds)
    : InvalidOperationException(CreateMessage(resourceIds))
{
    public IReadOnlyList<ResourceId> ResourceIds { get; } = resourceIds;

    private static string CreateMessage(IReadOnlyList<ResourceId> resourceIds) =>
        resourceIds.Count == 1
            ? $"Resource '{resourceIds[0]}' already exists."
            : $"{resourceIds.Count} resources already exist.";
}
