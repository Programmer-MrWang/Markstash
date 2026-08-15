namespace Markstash.Application.Abstractions;

public interface IAppPaths
{
    string DataDirectory { get; }

    string PreferencesFile { get; }
}
