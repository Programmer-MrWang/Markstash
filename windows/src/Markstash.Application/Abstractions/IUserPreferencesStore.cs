using Markstash.Domain.Preferences;
using Markstash.Application.Preferences;

namespace Markstash.Application.Abstractions;

public interface IUserPreferencesStore
{
    PreferencesLoadResult Load();

    void Save(UserPreferences preferences);
}
