using Markstash.Application;
using Markstash.Application.Abstractions;
using Markstash.Application.Preferences;
using Markstash.Domain.Preferences;
using Microsoft.Extensions.DependencyInjection;

namespace Markstash.Tests;

public sealed class PreferencesServiceTests
{
    [Fact]
    public void SetThemePersistsAndPublishesTheNewPreference()
    {
        var store = new InMemoryPreferencesStore();
        var services = new ServiceCollection();
        services.AddSingleton<IUserPreferencesStore>(store);
        services.AddMarkstashApplication();

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IPreferencesService>();
        UserPreferences? publishedPreferences = null;
        service.Changed += (_, preferences) => publishedPreferences = preferences;

        service.SetTheme(ThemePreference.Dark);

        Assert.Equal(ThemePreference.Dark, service.Current.Theme);
        Assert.Equal(ThemePreference.Dark, store.Stored.Theme);
        Assert.Equal(service.Current, publishedPreferences);
    }

    private sealed class InMemoryPreferencesStore : IUserPreferencesStore
    {
        public UserPreferences Stored { get; private set; } = UserPreferences.Default;

        public UserPreferences Load() => Stored;

        public void Save(UserPreferences preferences) => Stored = preferences;
    }
}
