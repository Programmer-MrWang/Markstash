using Markstash.Domain.Resources;

namespace Markstash.Application.Resources;

public interface IResourceRepository
{
    Task<ResourceRecord?> GetAsync(
        ResourceId resourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceRecord>> ListAsync(
        ResourceQuery query,
        CancellationToken cancellationToken = default);

    Task<ResourceBatchWriteResult> UpsertAsync(
        IReadOnlyCollection<ResourceRecord> resources,
        bool overwriteExisting,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        ResourceId resourceId,
        CancellationToken cancellationToken = default);
}
