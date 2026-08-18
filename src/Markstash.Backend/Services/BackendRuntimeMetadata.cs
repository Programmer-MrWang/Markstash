using System.Reflection;

namespace Markstash.Backend.Services;

internal sealed class BackendRuntimeMetadata
{
    public BackendRuntimeMetadata()
    {
        var assembly = typeof(BackendRuntimeMetadata).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var metadataSeparator = informationalVersion.IndexOf('+');
            Version = metadataSeparator < 0
                ? informationalVersion
                : informationalVersion[..metadataSeparator];
            return;
        }

        Version = assembly.GetName().Version?.ToString(3) ?? "unknown";
    }

    public string Version { get; }
}
