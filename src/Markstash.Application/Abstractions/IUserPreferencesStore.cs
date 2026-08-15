using Markstash.Domain.Preferences;

namespace Markstash.Application.Abstractions;

public interface IUserPreferencesStore
{
    UserPreferences Load();

    void Save(UserPreferences preferences);
}
