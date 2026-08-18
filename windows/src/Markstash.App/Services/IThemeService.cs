using Markstash.Domain.Preferences;

namespace Markstash.App.Services;

public interface IThemeService
{
    ThemePreference Current { get; }

    void Apply(ThemePreference preference);
}
