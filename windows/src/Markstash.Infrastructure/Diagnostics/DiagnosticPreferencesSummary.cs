using System.Text.Json;

namespace Markstash.Infrastructure.Diagnostics;

internal sealed record DiagnosticPreferencesSummary(
    string DocumentStatus,
    string? Format,
    int? SchemaVersion,
    string Theme)
{
    private const long MaximumPreferencesFileBytes = 1024 * 1024;

    private static readonly string[] SupportedThemes = ["System", "Light", "Dark"];

    public static DiagnosticPreferencesSummary Read(
        string preferencesFile,
        string backupFile)
    {
        if (TryRead(preferencesFile, "Primary", out var primary))
        {
            return primary;
        }

        if (TryRead(backupFile, "Backup", out var backup))
        {
            return backup;
        }

        var hasAnyDocument = File.Exists(preferencesFile) || File.Exists(backupFile);
        return new(
            hasAnyDocument ? "Invalid" : "Missing",
            Format: null,
            SchemaVersion: null,
            Theme: "Unknown");
    }

    private static bool TryRead(
        string path,
        string source,
        out DiagnosticPreferencesSummary summary)
    {
        summary = null!;
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length > MaximumPreferencesFileBytes)
            {
                return false;
            }

            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var root = document.RootElement;
            if (TryGetProperty(root, "schemaVersion", out var schemaElement))
            {
                if (!schemaElement.TryGetInt32(out var schemaVersion) ||
                    schemaVersion < 1 ||
                    !TryGetProperty(root, "preferences", out var preferences) ||
                    preferences.ValueKind != JsonValueKind.Object ||
                    !TryReadTheme(preferences, out var theme))
                {
                    return false;
                }

                summary = new(
                    $"{source}Available",
                    "Versioned",
                    schemaVersion,
                    theme);
                return true;
            }

            if (!TryReadTheme(root, out var legacyTheme))
            {
                return false;
            }

            summary = new(
                $"{source}Available",
                "Legacy",
                SchemaVersion: null,
                legacyTheme);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadTheme(JsonElement element, out string theme)
    {
        theme = "Unknown";
        if (!TryGetProperty(element, "theme", out var themeElement) ||
            themeElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var candidate = themeElement.GetString();
        var supportedTheme = SupportedThemes.FirstOrDefault(value => value.Equals(
            candidate,
            StringComparison.OrdinalIgnoreCase));
        if (supportedTheme is null)
        {
            return false;
        }

        theme = supportedTheme;
        return true;
    }

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
