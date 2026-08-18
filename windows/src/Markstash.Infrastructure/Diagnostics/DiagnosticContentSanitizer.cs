using System.Text.RegularExpressions;
using Markstash.Application.Abstractions;
using Markstash.Infrastructure.Logging;

namespace Markstash.Infrastructure.Diagnostics;

internal sealed partial class DiagnosticContentSanitizer
{
    private readonly string[] _privatePaths;

    public DiagnosticContentSanitizer(IAppPaths paths)
    {
        _privatePaths = new[]
            {
                paths.CrashReportsDirectory,
                paths.ConfigurationDirectory,
                paths.DatabaseDirectory,
                paths.TemporaryDirectory,
                paths.BackupsDirectory,
                paths.CacheDirectory,
                paths.StateDirectory,
                paths.LogsDirectory,
                paths.DataDirectory,
                paths.RootDirectory,
            }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .SelectMany(path => new[]
            {
                Path.GetFullPath(path),
                Path.GetFullPath(path).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(path => path.Length)
            .ToArray();
    }

    public string Sanitize(string value)
    {
        var sanitized = value;
        foreach (var privatePath in _privatePaths)
        {
            sanitized = sanitized.Replace(
                privatePath,
                "<app-path>",
                StringComparison.OrdinalIgnoreCase);
        }

        sanitized = BearerTokenPattern().Replace(sanitized, "Bearer ***");
        sanitized = JwtPattern().Replace(sanitized, "***");
        sanitized = LogSanitizer.Sanitize(sanitized);
        sanitized = JsonSecretPattern().Replace(sanitized, "$1***");
        sanitized = JsonSensitivePayloadPattern().Replace(sanitized, "$1***$2");
        sanitized = PlainSensitivePayloadPattern().Replace(sanitized, "$1***");
        sanitized = FileUriPattern().Replace(sanitized, "<absolute-path>");
        sanitized = WindowsAbsolutePathPattern().Replace(sanitized, "<absolute-path>");
        return PosixAbsolutePathPattern().Replace(sanitized, "<absolute-path>");
    }

    [GeneratedRegex(
        "(?i)([\"']?(?:password|access[_-]?token|refresh[_-]?token|token|secret|authorization|api[_-]?key)[\"']?\\s*[:=]\\s*[\"']?)([^\"',;\\s}\\]]+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex JsonSecretPattern();

    [GeneratedRegex(
        "(?i)([\"']?(?:resource[_-]?(?:content|record)|attachment(?:[_-]?content)?|raw[_-]?preferences|database|content)[\"']?\\s*:\\s*[\"'])(?:\\\\.|[^\"\\\\])*([\"'])",
        RegexOptions.CultureInvariant)]
    private static partial Regex JsonSensitivePayloadPattern();

    [GeneratedRegex(
        "(?i)(\\b(?:resource[_-]?(?:content|record)|attachment(?:[_-]?content)?|raw[_-]?preferences|database|content)\\b\\s*[:=]\\s*)([^\\r\\n,;]+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex PlainSensitivePayloadPattern();

    [GeneratedRegex(
        "(?i)\\bfile://[^\\s\"'<>|]+",
        RegexOptions.CultureInvariant)]
    private static partial Regex FileUriPattern();

    [GeneratedRegex(
        "(?i)\\bBearer\\s+[A-Za-z0-9._~+/=-]+",
        RegexOptions.CultureInvariant)]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex(
        "\\beyJ[A-Za-z0-9_-]{10,}\\.[A-Za-z0-9_-]{10,}\\.[A-Za-z0-9_-]{10,}\\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex JwtPattern();

    [GeneratedRegex(
        "(?i)(?<![A-Za-z0-9])(?:[A-Z]:[\\\\/]|\\\\\\\\)[^\\r\\n\"'<>|]*",
        RegexOptions.CultureInvariant)]
    private static partial Regex WindowsAbsolutePathPattern();

    [GeneratedRegex(
        "(?<![:/A-Za-z0-9])/(?!/)[^\\r\\n\"'<>|]*",
        RegexOptions.CultureInvariant)]
    private static partial Regex PosixAbsolutePathPattern();
}
