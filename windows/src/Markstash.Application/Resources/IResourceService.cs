using Markstash.Domain.Resources;

namespace Markstash.Application.Resources;

public interface IResourceService
{
    Task<ResourceRecord> CreateAsync(
        CreateResourceRequest request,
        CancellationToken cancellationToken = default);

    Task<ResourceRecord?> GetAsync(
        ResourceId resourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceRecord>> ListAsync(
        ResourceQuery query,
        CancellationToken cancellationToken = default);

    Task<ResourceRecord> UpdateAsync(
        UpdateResourceRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        ResourceId resourceId,
        CancellationToken cancellationToken = default);
}
