namespace Markstash.Application.Abstractions;

public interface IPlatformInfo
{
    string OperatingSystem { get; }

    string Architecture { get; }

    string Framework { get; }

    bool IsMobile { get; }
}
