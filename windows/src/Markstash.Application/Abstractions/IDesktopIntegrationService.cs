namespace Markstash.Application.Abstractions;

public interface IDesktopIntegrationService
{
    bool IsSupported { get; }

    string CreateDesktopShortcut();

    void OpenLogsDirectory();

    void OpenApplicationDirectory();
}
