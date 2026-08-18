using System.Runtime.InteropServices;

namespace Markstash.Backend.Runtime;

internal sealed class ServerRuntimeState(TimeProvider timeProvider)
{
    public DateTimeOffset StartedAtUtc { get; } = timeProvider.GetUtcNow();

    public int ProcessId { get; } = Environment.ProcessId;

    public string OperatingSystem { get; } = RuntimeInformation.OSDescription;

    public string Architecture { get; } =
        RuntimeInformation.ProcessArchitecture.ToString();

    public string Framework { get; } = RuntimeInformation.FrameworkDescription;
}
