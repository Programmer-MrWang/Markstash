using Markstash.Application;
using Markstash.Application.Abstractions;
using Markstash.Application.Preferences;
using Markstash.Domain.Preferences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Markstash.Tests;

public sealed class PreferencesServiceTests
{
    [Fact]
    public void SetThemePersistsAndPublishesTheNewPreference()
    {
        var store = new InMemoryPreferencesStore();
        using var provider = CreateProvider(store);
        var service = provider.GetRequiredService<IPreferencesService>();
        UserPreferences? publishedPreferences = null;
        service.Changed += (_, preferences) => publishedPreferences = preferences;

        var saved = service.SetTheme(ThemePreference.Dark);

        Assert.True(saved);
        Assert.Equal(ThemePreference.Dark, service.Current.Theme);
        Assert.Equal(ThemePreference.Dark, store.Stored.Theme);
        Assert.Equal(service.Current, publishedPreferences);
        Assert.Null(service.LastPersistenceError);
    }

    [Fact]
    public void SetThemeRollsBackMemoryStateWhenSavingFails()
    {
        const string errorMessage = "The preferences file is read-only.";
        var store = new InMemoryPreferencesStore(new IOException(errorMessage));
        using var provider = CreateProvider(store);
        var service = provider.GetRequiredService<IPreferencesService>();
        UserPreferences? publishedPreferences = null;
        string? publishedError = null;
        service.Changed += (_, preferences) => publishedPreferences = preferences;
        service.PersistenceFailed += (_, message) => publishedError = message;

        var saved = service.SetTheme(ThemePreference.Dark);

        Assert.False(saved);
        Assert.Equal(UserPreferences.Default, service.Current);
        Assert.Equal(UserPreferences.Default, store.Stored);
        Assert.Null(publishedPreferences);
        Assert.Equal(errorMessage, service.LastPersistenceError);
        Assert.Equal(errorMessage, publishedError);
    }

    [Fact]
    public void ObserverFailuresDoNotUndoACompletedPreferenceWrite()
    {
        var store = new InMemoryPreferencesStore();
        using var provider = CreateProvider(store);
        var service = provider.GetRequiredService<IPreferencesService>();
        service.Changed += (_, _) => throw new InvalidOperationException("observer failure");

        Assert.True(service.SetTheme(ThemePreference.Dark));
        Assert.Equal(ThemePreference.Dark, service.Current.Theme);
        Assert.Equal(ThemePreference.Dark, store.Stored.Theme);
    }

    private static ServiceProvider CreateProvider(IUserPreferencesStore store)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUserPreferencesStore>(store);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddMarkstashApplication();
        return services.BuildServiceProvider();
    }

    private sealed class InMemoryPreferencesStore(Exception? saveException = null) : IUserPreferencesStore
    {
        private readonly Exception? _saveException = saveException;

        public UserPreferences Stored { get; private set; } = UserPreferences.Default;

        public PreferencesLoadResult Load() =>
            new(Stored, PreferencesLoadStatus.Loaded);

        public void Save(UserPreferences preferences)
        {
            if (_saveException is not null)
            {
                throw _saveException;
            }

            Stored = preferences;
        }
    }
}
