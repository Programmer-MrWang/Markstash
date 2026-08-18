using Markstash.Domain.Preferences;

namespace Markstash.Application.Preferences;

public interface IPreferencesService
{
    UserPreferences Current { get; }

    PreferencesLoadStatus LoadStatus { get; }

    string? LastPersistenceError { get; }

    bool IsWritable { get; }

    event EventHandler<UserPreferences>? Changed;

    event EventHandler<string>? PersistenceFailed;

    bool SetTheme(ThemePreference theme);
}
