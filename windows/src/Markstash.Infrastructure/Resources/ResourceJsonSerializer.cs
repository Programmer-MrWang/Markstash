using System.Text.Json;
using System.Text.Json.Serialization;

namespace Markstash.Infrastructure.Resources;

internal static class ResourceJsonSerializer
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };
}
