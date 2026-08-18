namespace Markstash.Domain.Resources;

public readonly record struct ResourceId
{
    public ResourceId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static ResourceId New() => new(Guid.NewGuid().ToString("D"));

    public override string ToString() => Value ?? string.Empty;
}
