using Markstash.Domain.Resources;

namespace Markstash.Application.Resources;

public sealed class ResourceNotFoundException(ResourceId resourceId)
    : InvalidOperationException($"Resource '{resourceId}' does not exist.")
{
    public ResourceId ResourceId { get; } = resourceId;
}
