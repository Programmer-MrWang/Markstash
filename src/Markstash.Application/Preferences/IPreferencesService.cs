using Markstash.Domain.Preferences;

namespace Markstash.Application.Preferences;

public interface IPreferencesService
{
    UserPreferences Current { get; }

    event EventHandler<UserPreferences>? Changed;

    void SetTheme(ThemePreference theme);
}
