using Markstash.Domain.Resources;

namespace Markstash.Application.Resources;

public sealed record CreateResourceRequest(
    ResourceKind Kind,
    string Title,
    string? Source = null,
    string? Description = null,
    IReadOnlyList<string>? Tags = null,
    bool IsFavorite = false,
    string? ContentHash = null);
