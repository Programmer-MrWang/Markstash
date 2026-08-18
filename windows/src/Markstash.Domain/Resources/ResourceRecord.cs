using System.Collections.Immutable;

namespace Markstash.Domain.Resources;

public sealed record ResourceRecord
{
    public ResourceRecord(
        ResourceId id,
        ResourceKind kind,
        string title,
        string? source,
        string? description,
        IEnumerable<string>? tags,
        bool isFavorite,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? contentHash)
    {
        if (id == default)
        {
            throw new ArgumentException("A resource ID is required.", nameof(id));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown resource kind.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ValidateUtc(createdAtUtc, nameof(createdAtUtc));
        ValidateUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < createdAtUtc)
        {
            throw new ArgumentException(
                "The resource update time cannot precede its creation time.",
                nameof(updatedAtUtc));
        }

        Id = id;
        Kind = kind;
        Title = title.Trim();
        Source = NormalizeOptionalText(source);
        Description = NormalizeOptionalText(description);
        Tags = NormalizeTags(tags);
        IsFavorite = isFavorite;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        ContentHash = NormalizeOptionalText(contentHash);
    }

    public ResourceId Id { get; }

    public ResourceKind Kind { get; }

    public string Title { get; }

    public string? Source { get; }

    public string? Description { get; }

    public ImmutableArray<string> Tags { get; }

    public bool IsFavorite { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; }

    public string? ContentHash { get; }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ImmutableArray<string> NormalizeTags(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            return [];
        }

        var normalized = ImmutableArray.CreateBuilder<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            var trimmed = tag.Trim();
            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized.ToImmutable();
    }

    private static void ValidateUtc(DateTimeOffset value, string parameterName)
    {
        if (value == default || value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Resource timestamps must be non-default UTC values.",
                parameterName);
        }
    }
}
