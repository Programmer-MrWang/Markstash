using System.Text.Json;

namespace Markstash.Infrastructure.Diagnostics;

internal sealed record DiagnosticCrashReport(
    string? TimestampUtc,
    string? Source,
    bool? IsTerminating,
    string? ApplicationVersion,
    string? Framework,
    string? OperatingSystem,
    string? Architecture,
    int? ProcessId,
    string? Exception)
{
    public static DiagnosticCrashReport? Read(
        string path,
        DiagnosticContentSanitizer sanitizer)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var root = document.RootElement;
        return new(
            ReadString(root, "timestampUtc", sanitizer),
            ReadString(root, "source", sanitizer),
            ReadBoolean(root, "isTerminating"),
            ReadString(root, "applicationVersion", sanitizer),
            ReadString(root, "framework", sanitizer),
            ReadString(root, "operatingSystem", sanitizer),
            ReadString(root, "architecture", sanitizer),
            ReadInteger(root, "processId"),
            ReadString(root, "exception", sanitizer));
    }

    private static string? ReadString(
        JsonElement root,
        string propertyName,
        DiagnosticContentSanitizer sanitizer) =>
        TryGetProperty(root, propertyName, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? sanitizer.Sanitize(value.GetString() ?? string.Empty)
            : null;

    private static bool? ReadBoolean(JsonElement root, string propertyName) =>
        TryGetProperty(root, propertyName, out var value) &&
        value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static int? ReadInteger(JsonElement root, string propertyName) =>
        TryGetProperty(root, propertyName, out var value) && value.TryGetInt32(out var result)
            ? result
            : null;

    private static bool TryGetProperty(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        value = default;
        var found = false;
        foreach (var property in element.EnumerateObject())
        {
            if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (found)
            {
                return false;
            }

            value = property.Value;
            found = true;
        }

        return found;
    }
}
