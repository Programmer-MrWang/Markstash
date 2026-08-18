namespace Markstash.Application.Runtime;

public enum AppLifecycleState
{
    Created,
    Starting,
    Running,
    Stopping,
    Stopped,
    Faulted,
}
