using System.Text.Json;
using System.Text.Json.Serialization;
using Markstash.Application.Abstractions;
using Markstash.Domain.Preferences;

namespace Markstash.Infrastructure.Preferences;

internal sealed class JsonUserPreferencesStore : IUserPreferencesStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IAppPaths _paths;

    public JsonUserPreferencesStore(IAppPaths paths)
    {
        _paths = paths;
    }

    public UserPreferences Load()
    {
        try
        {
            if (!File.Exists(_paths.PreferencesFile))
            {
                return UserPreferences.Default;
            }

            var json = File.ReadAllText(_paths.PreferencesFile);
            return JsonSerializer.Deserialize<UserPreferences>(json, SerializerOptions)
                ?? UserPreferences.Default;
        }
        catch (JsonException)
        {
            return UserPreferences.Default;
        }
        catch (IOException)
        {
            return UserPreferences.Default;
        }
        catch (UnauthorizedAccessException)
        {
            return UserPreferences.Default;
        }
    }

    public void Save(UserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        Directory.CreateDirectory(_paths.DataDirectory);

        var temporaryFile = _paths.PreferencesFile + ".tmp";
        var json = JsonSerializer.Serialize(preferences, SerializerOptions);
        File.WriteAllText(temporaryFile, json);
        File.Move(temporaryFile, _paths.PreferencesFile, overwrite: true);
    }
}
