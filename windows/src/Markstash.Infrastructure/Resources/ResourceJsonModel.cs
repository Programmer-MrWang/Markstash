using Markstash.Domain.Resources;

namespace Markstash.Infrastructure.Resources;

internal sealed record ResourceJsonModel(
    string Id,
    ResourceKind Kind,
    string Title,
    string? Source,
    string? Description,
    IReadOnlyList<string>? Tags,
    bool IsFavorite,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? ContentHash)
{
    public static ResourceJsonModel FromDomain(ResourceRecord resource) => new(
        resource.Id.Value,
        resource.Kind,
        resource.Title,
        resource.Source,
        resource.Description,
        resource.Tags,
        resource.IsFavorite,
        resource.CreatedAtUtc,
        resource.UpdatedAtUtc,
        resource.ContentHash);

    public ResourceRecord ToDomain() => new(
        new ResourceId(Id),
        Kind,
        Title,
        Source,
        Description,
        Tags,
        IsFavorite,
        CreatedAtUtc,
        UpdatedAtUtc,
        ContentHash);
}
