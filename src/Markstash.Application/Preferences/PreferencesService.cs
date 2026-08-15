using Markstash.Application.Abstractions;
using Markstash.Domain.Preferences;

namespace Markstash.Application.Preferences;

internal sealed class PreferencesService : IPreferencesService
{
    private readonly IUserPreferencesStore _store;

    public PreferencesService(IUserPreferencesStore store)
    {
        _store = store;
        Current = store.Load();
    }

    public UserPreferences Current { get; private set; }

    public event EventHandler<UserPreferences>? Changed;

    public void SetTheme(ThemePreference theme)
    {
        if (Current.Theme == theme)
        {
            return;
        }

        Current = Current with { Theme = theme };
        _store.Save(Current);
        Changed?.Invoke(this, Current);
    }
}
