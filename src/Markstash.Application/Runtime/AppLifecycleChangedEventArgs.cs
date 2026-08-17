namespace Markstash.Application.Runtime;

public sealed class AppLifecycleChangedEventArgs(
    AppLifecycleState previous,
    AppLifecycleState current) : EventArgs
{
    public AppLifecycleState Previous { get; } = previous;

    public AppLifecycleState Current { get; } = current;
}
