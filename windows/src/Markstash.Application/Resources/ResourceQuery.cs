using Markstash.Domain.Resources;

namespace Markstash.Application.Resources;

public sealed record ResourceQuery
{
    public const int DefaultLimit = 100;
    public const int MaximumLimit = 500;

    public ResourceQuery(
        string? text = null,
        IEnumerable<ResourceKind>? kinds = null,
        bool favoritesOnly = false,
        IEnumerable<string>? tags = null,
        int offset = 0,
        int limit = DefaultLimit)
    {
        var normalizedKinds = (kinds ?? []).Distinct().ToArray();
        if (normalizedKinds.Any(kind => !Enum.IsDefined(kind)))
        {
            throw new ArgumentOutOfRangeException(nameof(kinds), "The query contains an unknown resource kind.");
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");
        }

        if (limit is <= 0 or > MaximumLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                $"Limit must be between 1 and {MaximumLimit}.");
        }

        Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        Kinds = normalizedKinds;
        FavoritesOnly = favoritesOnly;
        Tags = (tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Offset = offset;
        Limit = limit;
    }

    public string? Text { get; }

    public IReadOnlyList<ResourceKind> Kinds { get; }

    public bool FavoritesOnly { get; }

    public IReadOnlyList<string> Tags { get; }

    public int Offset { get; }

    public int Limit { get; }

    public static ResourceQuery Default { get; } = new();
}
