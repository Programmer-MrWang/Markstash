using System.Text.RegularExpressions;

namespace Markstash.Infrastructure.Logging;

internal static partial class LogSanitizer
{
    public static string Sanitize(string value)
    {
        var sanitized = SecretPattern().Replace(value, "$1$2***");
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(userProfile)
            ? sanitized
            : sanitized.Replace(userProfile, "~", StringComparison.OrdinalIgnoreCase);
    }

    public static string? SanitizeProperty(string key, string? value)
    {
        if (value is null)
        {
            return null;
        }

        return SecretKeyPattern().IsMatch(key) ? "***" : Sanitize(value);
    }

    [GeneratedRegex(
        "(?i)(password|token|secret|authorization|api[_-]?key)(\\s*[:=]\\s*)([^\\s,;]+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex SecretPattern();

    [GeneratedRegex(
        "(?i)password|token|secret|authorization|api[_-]?key",
        RegexOptions.CultureInvariant)]
    private static partial Regex SecretKeyPattern();
}
