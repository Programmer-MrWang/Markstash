using System.Runtime.InteropServices;
using Markstash.Application.Abstractions;

namespace Markstash.Infrastructure.Paths;

internal sealed class RuntimePlatformInfo : IPlatformInfo
{
    public string OperatingSystem => RuntimeInformation.OSDescription;

    public string Architecture => RuntimeInformation.ProcessArchitecture.ToString();

    public string Framework => RuntimeInformation.FrameworkDescription;

    public bool IsMobile =>
        global::System.OperatingSystem.IsAndroid() ||
        global::System.OperatingSystem.IsIOS();
}
