namespace Markstash.Domain.Preferences;

public sealed record UserPreferences(ThemePreference Theme)
{
    public static UserPreferences Default { get; } = new(ThemePreference.System);
}
