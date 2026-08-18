using Markstash.App.Localization;
using Markstash.Application.Preferences;
using Markstash.Domain.Preferences;
using Markstash.Infrastructure;

namespace Markstash.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ProjectReferencesFollowTheLayeringRules()
    {
        AssertDoesNotReference(
            typeof(UserPreferences).Assembly,
            "Markstash.Application",
            "Markstash.Infrastructure",
            "Markstash.App");
        AssertDoesNotReference(
            typeof(IPreferencesService).Assembly,
            "Markstash.Infrastructure",
            "Markstash.App");
        AssertDoesNotReference(
            typeof(DependencyInjection).Assembly,
            "Markstash.App");

        Assert.Contains(
            typeof(AppStrings).Assembly.GetReferencedAssemblies(),
            reference => reference.Name == "Markstash.Application");
    }

    private static void AssertDoesNotReference(
        System.Reflection.Assembly assembly,
        params string[] forbiddenReferences)
    {
        var references = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(
            forbiddenReferences,
            forbidden => Assert.DoesNotContain(forbidden, references));
    }
}
