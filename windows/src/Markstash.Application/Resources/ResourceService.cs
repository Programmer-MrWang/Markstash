using Markstash.Domain.Resources;

namespace Markstash.Application.Resources;

internal sealed class ResourceService(IResourceRepository repository) : IResourceService
{
    public async Task<ResourceRecord> CreateAsync(
        CreateResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = DateTimeOffset.UtcNow;
        var resource = new ResourceRecord(
            ResourceId.New(),
            request.Kind,
            request.Title,
            request.Source,
            request.Description,
            request.Tags,
            request.IsFavorite,
            now,
            now,
            request.ContentHash);

        await repository.UpsertAsync(
            [resource],
            overwriteExisting: false,
            cancellationToken);
        return resource;
    }

    public Task<ResourceRecord?> GetAsync(
        ResourceId resourceId,
        CancellationToken cancellationToken = default) =>
        repository.GetAsync(resourceId, cancellationToken);

    public Task<IReadOnlyList<ResourceRecord>> ListAsync(
        ResourceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return repository.ListAsync(query, cancellationToken);
    }

    public async Task<ResourceRecord> UpdateAsync(
        UpdateResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await repository.GetAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(request.Id);
        var now = DateTimeOffset.UtcNow;
        if (now < existing.CreatedAtUtc)
        {
            now = existing.CreatedAtUtc;
        }

        var updated = new ResourceRecord(
            existing.Id,
            request.Kind,
            request.Title,
            request.Source,
            request.Description,
            request.Tags,
            request.IsFavorite,
            existing.CreatedAtUtc,
            now,
            request.ContentHash);

        await repository.UpsertAsync(
            [updated],
            overwriteExisting: true,
            cancellationToken);
        return updated;
    }

    public Task<bool> DeleteAsync(
        ResourceId resourceId,
        CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(resourceId, cancellationToken);
}
