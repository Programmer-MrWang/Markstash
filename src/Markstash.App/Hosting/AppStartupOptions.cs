namespace Markstash.App.Hosting;

public sealed record AppStartupOptions(
    IReadOnlyList<string> Arguments,
    bool Verbose,
    string? DataDirectory,
    Uri? LaunchUri,
    IReadOnlyList<string> UnrecognizedArguments)
{
    public static AppStartupOptions Default { get; } = new([], false, null, null, []);

    public static AppStartupOptions Parse(IEnumerable<string> arguments)
    {
        var args = arguments.ToArray();
        var unrecognized = new List<string>();
        string? dataDirectory = null;
        Uri? launchUri = null;
        var verbose = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument is "--verbose" or "-v")
            {
                verbose = true;
                continue;
            }

            if (argument.StartsWith("--data-dir=", StringComparison.Ordinal))
            {
                dataDirectory = argument["--data-dir=".Length..];
                continue;
            }

            if (argument == "--data-dir" && index + 1 < args.Length)
            {
                dataDirectory = args[++index];
                continue;
            }

            if (Uri.TryCreate(argument, UriKind.Absolute, out var uri) &&
                uri.Scheme.Equals("markstash", StringComparison.OrdinalIgnoreCase))
            {
                launchUri = uri;
                continue;
            }

            unrecognized.Add(argument);
        }

        return new(args, verbose, dataDirectory, launchUri, unrecognized);
    }

    internal void ApplyEnvironmentOverrides()
    {
        if (!string.IsNullOrWhiteSpace(DataDirectory))
        {
            Environment.SetEnvironmentVariable(
                "MARKSTASH_DATA_DIR",
                Path.GetFullPath(DataDirectory));
        }
    }
}
