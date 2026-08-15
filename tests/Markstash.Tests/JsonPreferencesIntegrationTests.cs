using Markstash.Application;
using Markstash.Application.Preferences;
using Markstash.Domain.Preferences;
using Markstash.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Markstash.Tests;

public sealed class JsonPreferencesIntegrationTests
{
    [Fact]
    public void PreferencesRoundTripThroughTheConfiguredDataDirectory()
    {
        var dataDirectory = Path.Combine(
            Path.GetTempPath(),
            "Markstash.Tests",
            Guid.NewGuid().ToString("N"));
        var previousOverride = Environment.GetEnvironmentVariable("MARKSTASH_DATA_DIR");

        try
        {
            Environment.SetEnvironmentVariable("MARKSTASH_DATA_DIR", dataDirectory);

            using (var firstProvider = CreateProvider())
            {
                firstProvider
                    .GetRequiredService<IPreferencesService>()
                    .SetTheme(ThemePreference.Dark);
            }

            using var secondProvider = CreateProvider();
            var reloaded = secondProvider.GetRequiredService<IPreferencesService>();

            Assert.Equal(ThemePreference.Dark, reloaded.Current.Theme);
            Assert.True(File.Exists(Path.Combine(dataDirectory, "preferences.json")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MARKSTASH_DATA_DIR", previousOverride);

            if (Directory.Exists(dataDirectory))
            {
                Directory.Delete(dataDirectory, recursive: true);
            }
        }
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddMarkstashApplication();
        services.AddMarkstashInfrastructure();
        return services.BuildServiceProvider();
    }
}
